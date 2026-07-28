using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos.Common;
using Entities.Dtos.Venue;
using Microsoft.EntityFrameworkCore;

namespace Business.Concrete
{
    public class VenueManager : IVenueService
    {
        private readonly IVenueDal _venueDal;

        public VenueManager(IVenueDal venueDal)
        {
            _venueDal = venueDal;
        }

        // Yalnız admin tərəfindən təsdiqlənmiş sarayları bütün detalları ilə qaytarır.
        // Zalların dolu günləri də buraya daxildir ki, ana səhifə tək sorğu ilə dolsun —
        // yalnız bu gündən sonrakı və boş olmayan günlər göndərilir (cavab kiçik qalsın deyə).
        public async Task<IDataResult<List<Venue>>> GetAllAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

            var query = await _venueDal.GetListAsync(v => v.IsApproved);
            var venues = await query
                .Include(v => v.Images)
                .Include(v => v.Halls)
                    .ThenInclude(h => h.Availability.Where(a => a.Date >= today && a.Status != AvailabilityStatus.Available))
                .Include(v => v.Menus)
                .Include(v => v.Features)
                .Include(v => v.Reviews)
                .ToListAsync();

            return new SuccessDataResult<List<Venue>>(venues);
        }

        // Ana səhifənin siyahısı: axtarış + filtr + sıralama + səhifələmə (sonsuz scroll üçün)
        public async Task<IDataResult<PagedResult<Venue>>> SearchAsync(VenueSearchDto dto)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            var page = dto.Page < 1 ? 1 : dto.Page;
            var pageSize = dto.PageSize is < 1 or > 50 ? 12 : dto.PageSize;

            var query = await _venueDal.GetListAsync(v => v.IsApproved);
            var filtered = query.AsQueryable();

            if (!string.IsNullOrWhiteSpace(dto.Q))
            {
                var term = $"%{dto.Q.Trim()}%";
                filtered = filtered.Where(v =>
                    EF.Functions.ILike(v.Name, term) || EF.Functions.ILike(v.Address, term));
            }

            if (!string.IsNullOrWhiteSpace(dto.City))
                filtered = filtered.Where(v => v.City.ToLower() == dto.City.ToLower());

            if (dto.MinPrice.HasValue)
                filtered = filtered.Where(v => v.Menus.Any(m => m.PricePerPerson >= dto.MinPrice));

            if (dto.MaxPrice.HasValue)
                filtered = filtered.Where(v => v.Menus.Any(m => m.PricePerPerson <= dto.MaxPrice));

            if (dto.Capacity.HasValue)
                filtered = filtered.Where(v => v.Halls.Any(h => h.Capacity >= dto.Capacity));

            // Seçilən özəlliklərin HAMISI sarayda olmalıdır
            if (dto.Features is { Count: > 0 })
                foreach (var feature in dto.Features)
                    filtered = filtered.Where(v => v.Features.Any(f => f.Name == feature));

            // Ümumi büdcə: ən azı bir menyu üçün qiymət × qonaq sayı büdcəyə sığmalıdır.
            // Qonaq sayı verilməyibsə, büdcə bir nəfərlik maksimum kimi qəbul olunur.
            if (dto.Budget.HasValue)
            {
                var guests = dto.Capacity is > 0 ? dto.Capacity.Value : 1;
                filtered = filtered.Where(v => v.Menus.Any(m => m.PricePerPerson * guests <= dto.Budget));
            }

            // Orta reytinq — rəyi olmayan saraylar bu filtrdə görünmür
            if (dto.MinRating.HasValue)
                filtered = filtered.Where(v =>
                    v.Reviews.Any() && v.Reviews.Average(r => (double)r.Rating) >= dto.MinRating);

            // seçilmiş gündə ən azı bir zalı boş (Booked/Held olmayan) olan saraylar
            if (dto.Date.HasValue)
                filtered = filtered.Where(v => v.Halls.Any(h =>
                    !h.Availability.Any(a => a.Date == dto.Date && a.Status != AvailabilityStatus.Available)));

            // Səhifələmədən əvvəl ümumi sayı götürürük ki, frontend "daha var?" sualına cavab tapsın
            var totalCount = await filtered.CountAsync();

            filtered = dto.Sort switch
            {
                "price-asc" => filtered.OrderBy(v => v.Menus.Min(m => (decimal?)m.PricePerPerson)).ThenBy(v => v.Id),
                "price-desc" => filtered.OrderByDescending(v => v.Menus.Min(m => (decimal?)m.PricePerPerson)).ThenBy(v => v.Id),
                _ => filtered.OrderByDescending(v => v.Reviews.Average(r => (double?)r.Rating) ?? 0).ThenBy(v => v.Id),
            };

            var venues = await filtered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(v => v.Images)
                .Include(v => v.Halls)
                    .ThenInclude(h => h.Availability.Where(a => a.Date >= today && a.Status != AvailabilityStatus.Available))
                .Include(v => v.Menus)
                .Include(v => v.Features)
                .Include(v => v.Reviews)
                .ToListAsync();

            var result = new PagedResult<Venue>
            {
                Items = venues,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return new SuccessDataResult<PagedResult<Venue>>(result);
        }

        // Filtr panelinin seçimlərini bazadakı real dataya görə qurur:
        // hansı şəhərlər var, hansı özəlliklər neçə sarayda var, qiymət və tutum aralığı nədir
        public async Task<IDataResult<VenueFilterOptionsDto>> GetFilterOptionsAsync()
        {
            var query = await _venueDal.GetListAsync(v => v.IsApproved);

            var cities = await query
                .GroupBy(v => v.City)
                .Select(g => new FilterOption { Name = g.Key, Count = g.Count() })
                .OrderByDescending(o => o.Count)
                .ToListAsync();

            // Yalnız ən azı 5 sarayda olan özəlliklər — tək-tük olanlar filtr kimi mənasızdır
            var features = await query
                .SelectMany(v => v.Features)
                .GroupBy(f => f.Name)
                .Select(g => new FilterOption { Name = g.Key, Count = g.Count() })
                .Where(o => o.Count >= 5)
                .OrderByDescending(o => o.Count)
                .ToListAsync();

            var prices = await query.SelectMany(v => v.Menus).Select(m => m.PricePerPerson).ToListAsync();
            var capacities = await query.SelectMany(v => v.Halls).Select(h => h.Capacity).ToListAsync();

            var options = new VenueFilterOptionsDto
            {
                Cities = cities,
                Features = features,
                MinPrice = prices.Count > 0 ? prices.Min() : 0,
                MaxPrice = prices.Count > 0 ? prices.Max() : 0,
                MinCapacity = capacities.Count > 0 ? capacities.Min() : 0,
                MaxCapacity = capacities.Count > 0 ? capacities.Max() : 0
            };

            return new SuccessDataResult<VenueFilterOptionsDto>(options);
        }

        // Tək sarayın bütün detallarını (şəkillər, zallar+media+dolu günlər, menyular+tərkib) qaytarır
        public async Task<IDataResult<Venue>> GetByIdAsync(int id)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

            var query = await _venueDal.GetListAsync(v => v.Id == id);
            var venue = await query
                .Include(v => v.Images)
                .Include(v => v.Halls).ThenInclude(h => h.Media)
                .Include(v => v.Halls)
                    .ThenInclude(h => h.Availability.Where(a => a.Date >= today && a.Status != AvailabilityStatus.Available))
                .Include(v => v.Menus).ThenInclude(m => m.Items)
                .Include(v => v.Features)
                .Include(v => v.Reviews)
                .FirstOrDefaultAsync();

            return venue is null
                ? new ErrorDataResult<Venue>("Saray tapılmadı.")
                : new SuccessDataResult<Venue>(venue);
        }

        // Owner-in özünə aid bütün sarayları (təsdiqli və ya təsdiqsiz) görməsi
        public async Task<IDataResult<List<Venue>>> GetMineAsync(int ownerId)
        {
            var query = await _venueDal.GetListAsync(v => v.OwnerId == ownerId);
            var venues = await query
                .Include(v => v.Images)
                .Include(v => v.Halls)
                .Include(v => v.Menus)
                .ToListAsync();

            return new SuccessDataResult<List<Venue>>(venues);
        }

        // Yeni sadlıq sarayı əlavə edir. Test mərhələsində avtomatik təsdiqlənir (IsApproved=true);
        // real istifadəyə keçəndə bunu false edib admin təsdiqini məcburi etmək kifayətdir.
        public async Task<IDataResult<Venue>> CreateAsync(CreateVenueDto dto, int ownerId)
        {
            var venue = new Venue
            {
                OwnerId = ownerId,
                Name = dto.Name,
                Description = dto.Description,
                City = dto.City,
                Address = dto.Address,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Phone = dto.Phone,
                IsApproved = true,
                CreatedAt = DateTime.UtcNow
            };

            await _venueDal.AddAsync(venue);
            return new SuccessDataResult<Venue>(venue, "Saray əlavə olundu.");
        }

        // Owner öz sarayının məlumatlarını redaktə edir
        public async Task<IResult> UpdateAsync(int id, UpdateVenueDto dto, int ownerId)
        {
            var venue = await _venueDal.GetAsync(v => v.Id == id && v.OwnerId == ownerId);
            if (venue is null)
                return new ErrorResult("Saray tapılmadı və ya sizə aid deyil.");

            venue.Name = dto.Name;
            venue.Description = dto.Description;
            venue.City = dto.City;
            venue.Address = dto.Address;
            venue.Latitude = dto.Latitude;
            venue.Longitude = dto.Longitude;
            venue.Phone = dto.Phone;

            await _venueDal.UpdateAsync(venue);
            return new SuccessResult("Saray yeniləndi.");
        }

        // Owner öz sarayını silir
        public async Task<IResult> DeleteAsync(int id, int ownerId)
        {
            var venue = await _venueDal.GetAsync(v => v.Id == id && v.OwnerId == ownerId);
            if (venue is null)
                return new ErrorResult("Saray tapılmadı və ya sizə aid deyil.");

            await _venueDal.DeleteAsync(venue);
            return new SuccessResult("Saray silindi.");
        }

        // Admin təsdiqi gözləyən sarayların siyahısı
        public async Task<IDataResult<List<Venue>>> GetPendingAsync()
        {
            var query = await _venueDal.GetListAsync(v => !v.IsApproved);
            var venues = await query.ToListAsync();
            return new SuccessDataResult<List<Venue>>(venues);
        }

        // Admin sarayı təsdiqləyir, bundan sonra saray ictimai siyahıda görünür
        public async Task<IResult> ApproveAsync(int id)
        {
            var venue = await _venueDal.GetAsync(v => v.Id == id);
            if (venue is null)
                return new ErrorResult("Saray tapılmadı.");

            venue.IsApproved = true;
            await _venueDal.UpdateAsync(venue);
            return new SuccessResult("Saray təsdiqləndi.");
        }
    }
}
