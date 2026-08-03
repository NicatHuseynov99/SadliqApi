using Business.Abstract;
using Business.Security.Jwt;
using Core.Utilities.Results;
using Core.Utilities.Security.Hashing;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos.Venue;
using Microsoft.EntityFrameworkCore;

namespace Business.Concrete
{
    // Saray müraciəti. Bütün addımlar bir yerdə yazılır: hesab (lazımdırsa),
    // saray, zallar, menyular, şəkillər. Saray təsdiqsiz yaradılır.
    public class VenueApplicationManager : IVenueApplicationService
    {
        private readonly IUserDal _userDal;
        private readonly IVenueDal _venueDal;
        private readonly IHallDal _hallDal;
        private readonly IMenuDal _menuDal;
        private readonly IVenueImageDal _imageDal;
        private readonly IVenueFeatureDal _featureDal;
        private readonly IHallMediaDal _hallMediaDal;
        private readonly ICityDal _cityDal;
        private readonly ITokenHelper _tokenHelper;
        private readonly IAuditContext _audit;

        public VenueApplicationManager(
            IUserDal userDal,
            IVenueDal venueDal,
            IHallDal hallDal,
            IMenuDal menuDal,
            IVenueImageDal imageDal,
            IVenueFeatureDal featureDal,
            IHallMediaDal hallMediaDal,
            ICityDal cityDal,
            ITokenHelper tokenHelper,
            IAuditContext audit)
        {
            _userDal = userDal;
            _venueDal = venueDal;
            _hallDal = hallDal;
            _menuDal = menuDal;
            _imageDal = imageDal;
            _featureDal = featureDal;
            _hallMediaDal = hallMediaDal;
            _cityDal = cityDal;
            _tokenHelper = tokenHelper;
            _audit = audit;
        }

        public async Task<IDataResult<VenueApplicationResultDto>> SubmitAsync(
            VenueApplicationDto dto, int? currentUserId)
        {
            var error = Validate(dto, currentUserId);
            if (error is not null)
                return new ErrorDataResult<VenueApplicationResultDto>(error);

            // 1. Hesab: ya mövcuddur, ya yenidən yaradılır
            User owner;
            string? token = null;

            if (currentUserId.HasValue)
            {
                var existing = await _userDal.GetAsync(u => u.Id == currentUserId.Value);
                if (existing is null)
                    return new ErrorDataResult<VenueApplicationResultDto>("Hesab tapılmadı.");

                // Müştəri saray əlavə edirsə rolu sahibə keçir
                if (existing.Role == UserRole.Customer)
                {
                    existing.Role = UserRole.Owner;
                    await _userDal.UpdateAsync(existing);
                }

                owner = existing;
            }
            else
            {
                var email = dto.Applicant!.Email.Trim().ToLower();

                var duplicate = await _userDal.GetAsync(u => u.Email == email);
                if (duplicate is not null)
                    return new ErrorDataResult<VenueApplicationResultDto>(
                        "Bu e-poçt artıq qeydiyyatdan keçib. Daxil olub sarayı əlavə edin.");

                HashingHelper.CreatePasswordHash(dto.Applicant.Password.Trim(), out var hash, out var salt);

                owner = new User
                {
                    FirstName = dto.Applicant.FirstName.Trim(),
                    LastName = dto.Applicant.LastName.Trim(),
                    Email = email,
                    Phone = dto.Applicant.Phone.Trim(),
                    Role = UserRole.Owner,
                    IsActive = true,
                    PasswordHash = hash,
                    PasswordSalt = salt,
                    CreatedAt = DateTime.UtcNow
                };

                await _userDal.AddAsync(owner);

                // Sahibkar müraciətdən sonra dərhal panelinə keçə bilsin
                token = _tokenHelper.CreateToken(owner).Token;
            }

            // 2. Saray — təsdiqsiz
            var venue = new Venue
            {
                OwnerId = owner.Id,
                Name = dto.Venue.Name.Trim(),
                Description = dto.Venue.Description?.Trim() ?? string.Empty,
                City = dto.Venue.City.Trim(),
                Address = dto.Venue.Address.Trim(),
                Phone = dto.Venue.Phone.Trim(),
                Latitude = dto.Venue.Latitude,
                Longitude = dto.Venue.Longitude,
                ParkingCapacity = dto.Venue.ParkingCapacity,
                HasValetParking = dto.Venue.HasValetParking,
                CoverImageUrl = Clean(dto.Venue.CoverImageUrl),
                VideoUrl = Clean(dto.Venue.VideoUrl),
                IsApproved = false,
                CreatedAt = DateTime.UtcNow
            };

            await _venueDal.AddAsync(venue);

            // 3. Şəkillər
            var order = 0;
            foreach (var url in dto.Venue.ImageUrls.Select(Clean).Where(u => u is not null))
            {
                await _imageDal.AddAsync(new VenueImage
                {
                    VenueId = venue.Id,
                    Url = url!,
                    IsCover = false,
                    SortOrder = order++
                });
            }

            // 4. Özəlliklər
            foreach (var name in dto.Venue.Features.Select(f => f?.Trim()).Where(f => !string.IsNullOrEmpty(f)).Distinct())
                await _featureDal.AddAsync(new VenueFeature { VenueId = venue.Id, Name = name! });

            // 5. Zallar — adı yazılmayan boş sətirlər atılır
            foreach (var hallDto in dto.Halls.Where(h => !string.IsNullOrWhiteSpace(h.Name)))
            {
                var hall = new Hall
                {
                    VenueId = venue.Id,
                    Name = hallDto.Name.Trim(),
                    Capacity = hallDto.Capacity,
                    MinPrice = hallDto.MinPrice,
                    Description = hallDto.Description?.Trim() ?? string.Empty
                };

                await _hallDal.AddAsync(hall);

                var mediaOrder = 0;
                foreach (var url in hallDto.ImageUrls.Select(Clean).Where(u => u is not null))
                {
                    await _hallMediaDal.AddAsync(new HallMedia
                    {
                        HallId = hall.Id,
                        Url = url!,
                        MediaType = MediaType.Image,
                        SortOrder = mediaOrder++
                    });
                }
            }

            // 6. Menyular — adı yazılmayanlar atılır
            foreach (var menuDto in dto.Menus.Where(m => !string.IsNullOrWhiteSpace(m.Name)))
            {
                await _menuDal.AddAsync(new Menu
                {
                    VenueId = venue.Id,
                    Name = menuDto.Name.Trim(),
                    PricePerPerson = menuDto.PricePerPerson,
                    Description = menuDto.Description?.Trim() ?? string.Empty,
                    ImageUrl = Clean(menuDto.ImageUrl)
                });
            }

            // 7. Şəhər kitabçada yoxdursa əlavə olunur — admin siyahını əl ilə yenilməsin
            await EnsureCityAsync(venue.City);

            _audit.SetActor(owner.Id, owner.FullName, (int)owner.Role);
            _audit.Set("VenueApplied",
                $"«{venue.Name}» sarayı üçün müraciət göndərdi ({dto.Halls.Count} zal, {dto.Menus.Count} menyu)",
                "Venue", venue.Id, venue.Id);

            var result = new VenueApplicationResultDto
            {
                VenueId = venue.Id,
                VenueName = venue.Name,
                Token = token,
                Missing = MissingRecommendations(dto)
            };

            return new SuccessDataResult<VenueApplicationResultDto>(result,
                "Müraciətiniz göndərildi. Yoxlanışdan sonra sarayınız saytda görünəcək.");
        }

        // Yalnız ən zəruri sahələr məcburidir: hesabı açmaq üçün lazım olanlar
        // və sarayın adı. Qalanını sahibkar sonra, panelindən tamamlaya bilər —
        // uzun forma müraciəti yarımçıq qoymağın ən çox rast gəlinən səbəbidir.
        private static string? Validate(VenueApplicationDto dto, int? currentUserId)
        {
            if (currentUserId is null)
            {
                var a = dto.Applicant;
                if (a is null)
                    return "Hesab məlumatları göndərilməyib.";

                if (string.IsNullOrWhiteSpace(a.FirstName))
                    return "Adınızı yazın.";

                if (string.IsNullOrWhiteSpace(a.Email) || !a.Email.Contains('@'))
                    return "E-poçt düzgün deyil.";

                if (string.IsNullOrWhiteSpace(a.Password) || a.Password.Trim().Length < 6)
                    return "Parol ən azı 6 simvol olmalıdır.";
            }

            if (string.IsNullOrWhiteSpace(dto.Venue.Name))
                return "Sarayın adı boş ola bilməz.";

            // Doldurulubsa düzgün olmalıdır — boş qalması isə problem deyil
            foreach (var hall in dto.Halls.Where(h => !string.IsNullOrWhiteSpace(h.Name)))
            {
                if (hall.Capacity <= 0)
                    return $"«{hall.Name}» zalının qonaq tutumu göstərilməlidir.";
            }

            foreach (var menu in dto.Menus.Where(m => !string.IsNullOrWhiteSpace(m.Name)))
            {
                if (menu.PricePerPerson <= 0)
                    return $"«{menu.Name}» menyusunun nəfərbaşı qiyməti göstərilməlidir.";
            }

            return null;
        }

        // Təsdiqdən əvvəl tamamlanması məsləhət görülən sahələr —
        // müraciəti bloklamır, sadəcə sahibkara xatırladılır
        private static List<string> MissingRecommendations(VenueApplicationDto dto)
        {
            var missing = new List<string>();

            if (string.IsNullOrWhiteSpace(dto.Venue.CoverImageUrl))
                missing.Add("Fon şəkli");

            if (dto.Venue.ImageUrls.Count == 0)
                missing.Add("Saray şəkilləri");

            if (string.IsNullOrWhiteSpace(dto.Venue.VideoUrl))
                missing.Add("Video");

            if (dto.Menus.Count == 0)
                missing.Add("Menyular");

            if (dto.Venue.Latitude is null || dto.Venue.Longitude is null)
                missing.Add("Xəritədəki yer");

            if (dto.Venue.ParkingCapacity is null)
                missing.Add("Parkinq tutumu");

            if (dto.Halls.Count(h => !string.IsNullOrWhiteSpace(h.Name)) == 0)
                missing.Add("Zallar");

            if (string.IsNullOrWhiteSpace(dto.Venue.City) || string.IsNullOrWhiteSpace(dto.Venue.Address))
                missing.Add("Şəhər və ünvan");

            if (string.IsNullOrWhiteSpace(dto.Venue.Phone))
                missing.Add("Əlaqə nömrəsi");

            return missing;
        }

        private async Task EnsureCityAsync(string city)
        {
            var query = (await _cityDal.GetListAsync()).IgnoreQueryFilters();
            var existing = await query.FirstOrDefaultAsync(c => c.Name.ToLower() == city.ToLower());

            if (existing is null)
            {
                await _cityDal.AddAsync(new City { Name = city, CreatedAt = DateTime.UtcNow });
                return;
            }

            if (existing.IsDeleted)
            {
                existing.IsDeleted = false;
                existing.DeletedAt = null;
                await _cityDal.UpdateAsync(existing);
            }
        }

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
