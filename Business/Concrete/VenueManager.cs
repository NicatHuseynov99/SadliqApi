using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework;
using Entities.Concrete;
using Entities.Dtos.Common;
using Entities.Dtos.Venue;
using Microsoft.EntityFrameworkCore;

namespace Business.Concrete
{
    public class VenueManager : IVenueService
    {
        private readonly IVenueDal _venueDal;
        private readonly IVenueAccessService _access;
        private readonly IVenueFeatureDal _featureDal;
        private readonly IVenueImageDal _imageDal;
        private readonly IAuditContext _audit;
        private readonly IPlatformSettingsService _settings;

        public VenueManager(
            IVenueDal venueDal,
            IVenueAccessService access,
            IVenueFeatureDal featureDal,
            IVenueImageDal imageDal,
            IAuditContext audit,
            IPlatformSettingsService settings)
        {
            _audit = audit;
            _settings = settings;
            _venueDal = venueDal;
            _access = access;
            _featureDal = featureDal;
            _imageDal = imageDal;
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

            filtered = ApplyTextSearch(filtered, dto.Q);

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

            // Parkinq tutumu göstərilməyən saraylar bu filtrdə görünmür —
            // «bilmirik» ilə «yoxdur» eyni şey deyil, amma müştəriyə söz verə bilmərik
            if (dto.MinParking.HasValue)
                filtered = filtered.Where(v => v.ParkingCapacity >= dto.MinParking);

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

        // Xəritə rejimi. Adi axtarışdan iki fərqi var:
        // 1) yalnız koordinatı olan saraylar qaytarılır;
        // 2) səhifələmə əvəzinə xəritənin görünən sahəsi (bbox) süzgəci işləyir.
        public async Task<IDataResult<VenueMapResultDto>> MapSearchAsync(VenueMapSearchDto dto)
        {
            var query = await _venueDal.GetListAsync(v => v.IsApproved);
            var filtered = ApplyCommonFilters(query, dto);

            // Süzgəcə uyğun gələnlərin hamısı — koordinatı olmayanlar da daxil
            var totalMatched = await filtered.CountAsync();

            var withCoordinates = filtered.Where(v => v.Latitude != null && v.Longitude != null);
            var mapped = await withCoordinates.CountAsync();

            // Xəritənin görünən sahəsi. Dünya xəritəsinin kənarını keçən hallarda
            // (məsələn Sakit okean üzərində) west > east olur — onu ayrıca yoxlayırıq.
            if (dto.North.HasValue && dto.South.HasValue && dto.East.HasValue && dto.West.HasValue)
            {
                var north = dto.North.Value;
                var south = dto.South.Value;
                var east = dto.East.Value;
                var west = dto.West.Value;

                withCoordinates = withCoordinates.Where(v =>
                    v.Latitude! <= north && v.Latitude >= south);

                withCoordinates = west <= east
                    ? withCoordinates.Where(v => v.Longitude! <= east && v.Longitude >= west)
                    : withCoordinates.Where(v => v.Longitude! >= west || v.Longitude <= east);
            }

            var limit = dto.Limit is < 1 or > 500 ? 200 : dto.Limit;

            // «Mənə yaxın» seçilibsə sıralama BAZADA olmalıdır — əks halda limit
            // təsadüfi sarayları kəsər və ən yaxınlar siyahıdan kənarda qalar.
            // Haversine SQL-ə tərcümə olunmur, ona görə kiçik məsafələrdə eyni
            // nəticəni verən sadə kvadrat fərq işlədilir; dəqiq km sonra hesablanır.
            if (dto.NearLat.HasValue && dto.NearLng.HasValue)
            {
                var lat = dto.NearLat.Value;
                var lng = dto.NearLng.Value;

                withCoordinates = withCoordinates.OrderBy(v =>
                    (v.Latitude!.Value - lat) * (v.Latitude.Value - lat) +
                    (v.Longitude!.Value - lng) * (v.Longitude.Value - lng));
            }

            var rows = await withCoordinates
                .Select(v => new
                {
                    v.Id,
                    v.Name,
                    v.City,
                    v.Address,
                    v.CoverImageUrl,
                    Lat = v.Latitude!.Value,
                    Lng = v.Longitude!.Value,
                    MinPrice = v.Menus.Min(m => (decimal?)m.PricePerPerson),
                    MaxCapacity = v.Halls.Max(h => (int?)h.Capacity),
                    v.ParkingCapacity,
                    Rating = v.Reviews.Average(r => (double?)r.Rating) ?? 0,
                    ReviewCount = v.Reviews.Count
                })
                .Take(limit)
                .ToListAsync();

            var items = rows.Select(r => new VenueMapItemDto
            {
                Id = r.Id,
                Name = r.Name,
                City = r.City,
                Address = r.Address,
                CoverImageUrl = r.CoverImageUrl,
                Latitude = r.Lat,
                Longitude = r.Lng,
                MinPrice = r.MinPrice,
                MaxCapacity = r.MaxCapacity,
                ParkingCapacity = r.ParkingCapacity,
                Rating = Math.Round(r.Rating, 2),
                ReviewCount = r.ReviewCount,
                DistanceKm = dto.NearLat.HasValue && dto.NearLng.HasValue
                    ? Math.Round(Distance(dto.NearLat.Value, dto.NearLng.Value, r.Lat, r.Lng), 1)
                    : null
            }).ToList();

            // «Mənə yaxın» seçilibsə ən yaxınlar öndə olsun
            if (dto.NearLat.HasValue && dto.NearLng.HasValue)
                items = items.OrderBy(i => i.DistanceKm).ToList();

            var result = new VenueMapResultDto
            {
                Items = items,
                TotalMatched = totalMatched,
                WithoutCoordinates = totalMatched - mapped
            };

            return new SuccessDataResult<VenueMapResultDto>(result);
        }

        // İki koordinat arasındakı məsafə (km) — Haversine düsturu.
        // Nəticə quş uçuşu məsafəsidir, yol məsafəsi deyil.
        private static double Distance(double lat1, double lng1, double lat2, double lng2)
        {
            const double earthRadiusKm = 6371;

            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLng = (lng2 - lng1) * Math.PI / 180;

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                    Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

            return earthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        // Axtarış qutusunun təklifləri: 5 saray + 3 şəhər.
        // Cavab yüngül olmalıdır — hər hərfdə çağırılır.
        public async Task<IDataResult<List<SearchSuggestionDto>>> SuggestAsync(string? q)
        {
            var suggestions = new List<SearchSuggestionDto>();

            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
                return new SuccessDataResult<List<SearchSuggestionDto>>(suggestions);

            var query = await _venueDal.GetListAsync(v => v.IsApproved);
            var term = $"%{q.Trim()}%";

            var venues = await query
                .Where(v => EF.Functions.Like(SearchFunctions.Normalize(v.Name), SearchFunctions.Normalize(term)))
                .OrderByDescending(v => v.Reviews.Count)
                .Take(5)
                .Select(v => new SearchSuggestionDto
                {
                    Type = "venue",
                    Text = v.Name,
                    VenueId = v.Id,
                    City = v.City,
                    ImageUrl = v.CoverImageUrl
                })
                .ToListAsync();

            var cities = await query
                .Where(v => EF.Functions.Like(SearchFunctions.Normalize(v.City), SearchFunctions.Normalize(term)))
                .GroupBy(v => v.City)
                .Select(g => new SearchSuggestionDto
                {
                    Type = "city",
                    Text = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(c => c.Count)
                .Take(3)
                .ToListAsync();

            suggestions.AddRange(venues);
            suggestions.AddRange(cities);

            return new SuccessDataResult<List<SearchSuggestionDto>>(suggestions);
        }

        // Mətn axtarışı. İki məsələni həll edir:
        // 1) diakritika — «Zumrud» yazan «Zümrüd Saray»ı tapmalıdır (unaccent);
        // 2) əhatə — müştəri sarayın adını bilməyə bilər, amma zalın, menyunun
        //    və ya özəlliyin adını yazır.
        private static IQueryable<Venue> ApplyTextSearch(IQueryable<Venue> query, string? search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return query;

            // az_normalize() həm hərfləri sadələşdirir, həm kiçik hərfə salır —
            // ona görə adi LIKE kifayətdir
            var term = $"%{search.Trim()}%";

            return query.Where(v =>
                EF.Functions.Like(SearchFunctions.Normalize(v.Name), SearchFunctions.Normalize(term)) ||
                EF.Functions.Like(SearchFunctions.Normalize(v.Address), SearchFunctions.Normalize(term)) ||
                EF.Functions.Like(SearchFunctions.Normalize(v.City), SearchFunctions.Normalize(term)) ||
                v.Halls.Any(h => EF.Functions.Like(SearchFunctions.Normalize(h.Name), SearchFunctions.Normalize(term))) ||
                v.Menus.Any(m => EF.Functions.Like(SearchFunctions.Normalize(m.Name), SearchFunctions.Normalize(term))) ||
                v.Features.Any(f => EF.Functions.Like(SearchFunctions.Normalize(f.Name), SearchFunctions.Normalize(term))));
        }

        // Siyahı və xəritə eyni süzgəcləri işlətməlidir — məntiq bir yerdə saxlanılır
        private static IQueryable<Venue> ApplyCommonFilters(IQueryable<Venue> query, VenueMapSearchDto dto)
        {
            var filtered = query;

            filtered = ApplyTextSearch(filtered, dto.Q);

            if (!string.IsNullOrWhiteSpace(dto.City))
                filtered = filtered.Where(v => v.City.ToLower() == dto.City.ToLower());

            if (dto.MinPrice.HasValue)
                filtered = filtered.Where(v => v.Menus.Any(m => m.PricePerPerson >= dto.MinPrice));

            if (dto.MaxPrice.HasValue)
                filtered = filtered.Where(v => v.Menus.Any(m => m.PricePerPerson <= dto.MaxPrice));

            if (dto.Capacity.HasValue)
                filtered = filtered.Where(v => v.Halls.Any(h => h.Capacity >= dto.Capacity));

            if (dto.Features is { Count: > 0 })
                foreach (var feature in dto.Features)
                    filtered = filtered.Where(v => v.Features.Any(f => f.Name == feature));

            if (dto.Budget.HasValue)
            {
                var guests = dto.Capacity is > 0 ? dto.Capacity.Value : 1;
                filtered = filtered.Where(v => v.Menus.Any(m => m.PricePerPerson * guests <= dto.Budget));
            }

            if (dto.MinRating.HasValue)
                filtered = filtered.Where(v =>
                    v.Reviews.Any() && v.Reviews.Average(r => (double)r.Rating) >= dto.MinRating);

            if (dto.MinParking.HasValue)
                filtered = filtered.Where(v => v.ParkingCapacity >= dto.MinParking);

            if (dto.Date.HasValue)
                filtered = filtered.Where(v => v.Halls.Any(h =>
                    !h.Availability.Any(a => a.Date == dto.Date && a.Status != AvailabilityStatus.Available)));

            return filtered;
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

            // Az sarayda olan özəlliklər filtr kimi mənasızdır. Həddi admin paneldən
            // dəyişmək olar — ona görə sabit yazmırıq.
            var minCount = await _settings.GetIntAsync("feature_filter_min_count", 5);

            var features = await query
                .SelectMany(v => v.Features)
                .GroupBy(f => f.Name)
                .Select(g => new FilterOption { Name = g.Key, Count = g.Count() })
                .Where(o => o.Count >= minCount)
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
            // Sahib öz saraylarını, administrator isə təyin olunduğu sarayları görür
            var venueIds = await _access.GetManagedVenueIdsAsync(ownerId);
            var query = await _venueDal.GetListAsync(v => venueIds.Contains(v.Id));
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
                VideoUrl = dto.VideoUrl,
                ParkingCapacity = dto.ParkingCapacity,
                HasValetParking = dto.HasValetParking,
                CoverImageUrl = dto.CoverImageUrl,
                IsApproved = true,
                CreatedAt = DateTime.UtcNow
            };

            // Özəlliklər sarayla birlikdə yazılır
            foreach (var name in (dto.Features ?? new List<string>()).Where(f => !string.IsNullOrWhiteSpace(f)))
                venue.Features.Add(new VenueFeature { Name = name.Trim() });

            // Kapak şəkli qalereyaya da düşür ki, detal səhifəsində görünsün
            if (!string.IsNullOrWhiteSpace(dto.CoverImageUrl))
                venue.Images.Add(new VenueImage { Url = dto.CoverImageUrl, IsCover = true, SortOrder = 0 });

            await _venueDal.AddAsync(venue);

            _audit.Set("VenueCreated", $"«{venue.Name}» sarayını əlavə etdi ({venue.City})",
                "Venue", venue.Id, venue.Id);

            return new SuccessDataResult<Venue>(venue, "Saray əlavə olundu.");
        }

        // Owner öz sarayının məlumatlarını redaktə edir
        public async Task<IResult> UpdateAsync(int id, UpdateVenueDto dto, int ownerId)
        {
            var venue = await _venueDal.GetAsync(v => v.Id == id);
            if (venue is null)
                return new ErrorResult("Saray tapılmadı.");

            // Sahiblik yoxlaması mərkəzi servisdədir — admin bütün saraylara çıxışlıdır
            if (!await _access.CanModifyAsync(id, ownerId))
                return new ErrorResult("Bu saray sizə aid deyil.");

            venue.Name = dto.Name;
            venue.Description = dto.Description;
            venue.City = dto.City;
            venue.Address = dto.Address;
            venue.Latitude = dto.Latitude;
            venue.Longitude = dto.Longitude;
            venue.Phone = dto.Phone;
            venue.VideoUrl = dto.VideoUrl;
            venue.ParkingCapacity = dto.ParkingCapacity;
            venue.HasValetParking = dto.HasValetParking;

            if (dto.CoverImageUrl is not null)
                venue.CoverImageUrl = dto.CoverImageUrl;

            await _venueDal.UpdateAsync(venue);

            // Özəlliklər siyahısı göndərilibsə, köhnələr silinib yenisi yazılır
            if (dto.Features is not null)
            {
                var oldQuery = await _featureDal.GetListAsync(f => f.VenueId == id);
                foreach (var old in await oldQuery.ToListAsync())
                    await _featureDal.DeleteAsync(old);

                foreach (var name in dto.Features.Where(f => !string.IsNullOrWhiteSpace(f)))
                    await _featureDal.AddAsync(new VenueFeature { VenueId = id, Name = name.Trim() });
            }

            // Kapak şəkli dəyişibsə qalereyadakı əsas şəkil də yenilənir
            if (!string.IsNullOrWhiteSpace(dto.CoverImageUrl))
            {
                var cover = await _imageDal.GetAsync(i => i.VenueId == id && i.IsCover);

                if (cover is null)
                    await _imageDal.AddAsync(new VenueImage
                    {
                        VenueId = id,
                        Url = dto.CoverImageUrl,
                        IsCover = true,
                        SortOrder = 0
                    });
                else if (cover.Url != dto.CoverImageUrl)
                {
                    cover.Url = dto.CoverImageUrl;
                    await _imageDal.UpdateAsync(cover);
                }
            }

            _audit.Set("VenueUpdated", $"«{venue.Name}» sarayının məlumatlarını yenilədi",
                "Venue", venue.Id, venue.Id);

            return new SuccessResult("Saray yeniləndi.");
        }

        // Owner öz sarayını silir
        public async Task<IResult> DeleteAsync(int id, int ownerId)
        {
            var venue = await _venueDal.GetAsync(v => v.Id == id);
            if (venue is null)
                return new ErrorResult("Saray tapılmadı.");

            // Sahiblik yoxlaması mərkəzi servisdədir — admin bütün saraylara çıxışlıdır
            if (!await _access.CanModifyAsync(id, ownerId))
                return new ErrorResult("Bu saray sizə aid deyil.");

            _audit.Set("VenueDeleted", $"«{venue.Name}» sarayını sildi", "Venue", venue.Id, venue.Id);

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
