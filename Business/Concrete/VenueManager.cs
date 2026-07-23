using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
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

        // Yalnız admin tərəfindən təsdiqlənmiş sarayları, şəkil/zal/menyuları ilə birgə qaytarır
        public async Task<IDataResult<List<Venue>>> GetAllAsync()
        {
            var query = await _venueDal.GetListAsync(v => v.IsApproved);
            var venues = await query
                .Include(v => v.Images)
                .Include(v => v.Halls)
                .Include(v => v.Menus)
                .ToListAsync();

            return new SuccessDataResult<List<Venue>>(venues);
        }

        // Şəhər, tarix, qiymət və tutuma görə təsdiqlənmiş saraylar arasında axtarış
        public async Task<IDataResult<List<Venue>>> SearchAsync(string? city, DateOnly? date, decimal? minPrice, decimal? maxPrice, int? capacity)
        {
            var query = await _venueDal.GetListAsync(v => v.IsApproved);
            var filtered = query
                .Include(v => v.Halls)
                .Include(v => v.Menus)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(city))
                filtered = filtered.Where(v => v.City.ToLower() == city.ToLower());

            if (minPrice.HasValue)
                filtered = filtered.Where(v => v.Menus.Any(m => m.PricePerPerson >= minPrice));

            if (maxPrice.HasValue)
                filtered = filtered.Where(v => v.Menus.Any(m => m.PricePerPerson <= maxPrice));

            if (capacity.HasValue)
                filtered = filtered.Where(v => v.Halls.Any(h => h.Capacity >= capacity));

            // seçilmiş gündə ən azı bir zalı boş (Booked/Held olmayan) olan saraylar
            if (date.HasValue)
                filtered = filtered.Where(v => v.Halls.Any(h =>
                    !h.Availability.Any(a => a.Date == date && a.Status != AvailabilityStatus.Available)));

            var venues = await filtered.ToListAsync();
            return new SuccessDataResult<List<Venue>>(venues);
        }

        // Tək sarayın bütün detallarını (şəkillər, zallar+media+dolu günlər, menyular+tərkib) qaytarır
        public async Task<IDataResult<Venue>> GetByIdAsync(int id)
        {
            var query = await _venueDal.GetListAsync(v => v.Id == id);
            var venue = await query
                .Include(v => v.Images)
                .Include(v => v.Halls).ThenInclude(h => h.Media)
                .Include(v => v.Halls).ThenInclude(h => h.Availability)
                .Include(v => v.Menus).ThenInclude(m => m.Items)
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
