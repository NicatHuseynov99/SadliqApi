using Business.Abstract;
using Core.Utilities.Results;
using Core.Utilities.Security.Hashing;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos.Staff;
using Microsoft.EntityFrameworkCore;

namespace Business.Concrete
{
    public class VenueStaffManager : IVenueStaffService
    {
        private readonly IVenueStaffDal _staffDal;
        private readonly IVenueDal _venueDal;
        private readonly IUserDal _userDal;
        private readonly IVenueAccessService _access;
        private readonly IVenueStaffHallDal _staffHallDal;
        private readonly IHallDal _hallDal;

        public VenueStaffManager(
            IVenueStaffDal staffDal,
            IVenueDal venueDal,
            IUserDal userDal,
            IVenueAccessService access,
            IVenueStaffHallDal staffHallDal,
            IHallDal hallDal)
        {
            _staffDal = staffDal;
            _venueDal = venueDal;
            _userDal = userDal;
            _access = access;
            _staffHallDal = staffHallDal;
            _hallDal = hallDal;
        }

        // Sahibin saraylarına təyin olunmuş bütün administratorlar
        // (SuperAdmin bütün sarayları idarə etdiyi üçün hamısını görür)
        public async Task<IDataResult<List<VenueStaffDto>>> GetMineAsync(int ownerId)
        {
            var managedIds = await _access.GetManagedVenueIdsAsync(ownerId);
            var venueQuery = await _venueDal.GetListAsync(v => managedIds.Contains(v.Id));
            var venues = await venueQuery.Select(v => new { v.Id, v.Name }).ToListAsync();
            var venueIds = venues.Select(v => v.Id).ToList();

            var staffQuery = await _staffDal.GetListAsync(s => venueIds.Contains(s.VenueId));
            var staffList = await staffQuery
                .Include(s => s.User)
                .Include(s => s.Halls).ThenInclude(h => h.Hall)
                .ToListAsync();

            var result = staffList.Select(s => new VenueStaffDto
            {
                Id = s.Id,
                VenueId = s.VenueId,
                VenueName = venues.FirstOrDefault(v => v.Id == s.VenueId)?.Name ?? string.Empty,
                UserId = s.UserId,
                FullName = s.User?.FullName ?? string.Empty,
                Email = s.User?.Email ?? string.Empty,
                Phone = s.User?.Phone ?? string.Empty,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt,
                HallIds = s.Halls.Select(h => h.HallId).ToList(),
                HallNames = s.Halls.Select(h => h.Hall?.Name ?? string.Empty).ToList(),
                CanConfirmReservations = s.CanConfirmReservations,
                CanEditCalendar = s.CanEditCalendar,
                CanSendOffers = s.CanSendOffers
            }).ToList();

            return new SuccessDataResult<List<VenueStaffDto>>(result);
        }

        // Administrator hesabı yaradır. Saray mütləq sorğunu göndərənin öz sarayı olmalıdır —
        // əks halda bir sahib başqasının sarayına administrator təyin edə bilərdi.
        public async Task<IDataResult<VenueStaffDto>> CreateAsync(CreateVenueStaffDto dto, int ownerId)
        {
            if (!await _access.CanModifyAsync(dto.VenueId, ownerId))
                return new ErrorDataResult<VenueStaffDto>("Bu saray sizə aid deyil.");

            if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
                return new ErrorDataResult<VenueStaffDto>("Ad və soyad boş ola bilməz.");

            if (string.IsNullOrWhiteSpace(dto.Email))
                return new ErrorDataResult<VenueStaffDto>("E-poçt boş ola bilməz.");

            if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
                return new ErrorDataResult<VenueStaffDto>("Parol ən azı 6 simvol olmalıdır.");

            var email = dto.Email.Trim();
            var existingUser = await _userDal.GetAsync(u => u.Email == email);

            var venue = await _venueDal.GetAsync(v => v.Id == dto.VenueId);

            User user;

            if (existingUser is not null)
            {
                // Mövcud hesab yalnız administrator rolundadırsa yenidən təyin oluna bilər
                if (existingUser.Role != UserRole.VenueAdmin)
                    return new ErrorDataResult<VenueStaffDto>("Bu e-poçt başqa hesaba aiddir.");

                var alreadyAssigned = await _staffDal.GetAsync(
                    s => s.VenueId == dto.VenueId && s.UserId == existingUser.Id);

                if (alreadyAssigned is not null)
                    return new ErrorDataResult<VenueStaffDto>("Bu administrator artıq həmin saraya təyin olunub.");

                user = existingUser;
            }
            else
            {
                HashingHelper.CreatePasswordHash(dto.Password, out var hash, out var salt);

                user = new User
                {
                    FirstName = dto.FirstName.Trim(),
                    LastName = dto.LastName.Trim(),
                    Email = email,
                    Phone = dto.Phone?.Trim() ?? string.Empty,
                    PasswordHash = hash,
                    PasswordSalt = salt,
                    Role = UserRole.VenueAdmin,
                    CreatedAt = DateTime.UtcNow
                };

                await _userDal.AddAsync(user);
            }

            var staff = new VenueStaff
            {
                VenueId = dto.VenueId,
                UserId = user.Id,
                CreatedByUserId = ownerId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            // Seçilmiş zallar təyinatla birlikdə yazılır (boşdursa bütün zallar deməkdir)
            foreach (var hallId in await FilterVenueHallsAsync(dto.VenueId, dto.HallIds))
                staff.Halls.Add(new VenueStaffHall { HallId = hallId });

            await _staffDal.AddAsync(staff);

            return new SuccessDataResult<VenueStaffDto>(new VenueStaffDto
            {
                Id = staff.Id,
                VenueId = staff.VenueId,
                VenueName = venue?.Name ?? string.Empty,
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                IsActive = true,
                CreatedAt = staff.CreatedAt
            }, "Administrator əlavə olundu.");
        }

        // Administratorun ad, soyad və telefonunu yeniləyir
        public async Task<IResult> UpdateAsync(int staffId, UpdateVenueStaffDto dto, int ownerId)
        {
            if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
                return new ErrorResult("Ad və soyad boş ola bilməz.");

            var staff = await _staffDal.GetAsync(s => s.Id == staffId);
            if (staff is null)
                return new ErrorResult("Administrator tapılmadı.");

            if (!await _access.CanModifyAsync(staff.VenueId, ownerId))
                return new ErrorResult("Bu administrator sizin sarayınıza aid deyil.");

            var user = await _userDal.GetAsync(u => u.Id == staff.UserId);
            if (user is null)
                return new ErrorResult("İstifadəçi tapılmadı.");

            user.FirstName = dto.FirstName.Trim();
            user.LastName = dto.LastName.Trim();
            user.Phone = dto.Phone?.Trim() ?? string.Empty;
            await _userDal.UpdateAsync(user);

            return new SuccessResult("Administratorun məlumatları yeniləndi.");
        }

        // Administratorun işləyə biləcəyi zalları yenidən təyin edir
        public async Task<IResult> SetHallsAsync(int staffId, SetStaffHallsDto dto, int ownerId)
        {
            var staff = await _staffDal.GetAsync(s => s.Id == staffId);
            if (staff is null)
                return new ErrorResult("Administrator tapılmadı.");

            if (!await _access.CanModifyAsync(staff.VenueId, ownerId))
                return new ErrorResult("Bu administrator sizin sarayınıza aid deyil.");

            // Köhnə təyinatlar silinir
            var oldQuery = await _staffHallDal.GetListAsync(h => h.VenueStaffId == staffId);
            foreach (var old in await oldQuery.ToListAsync())
                await _staffHallDal.DeleteAsync(old);

            var hallIds = await FilterVenueHallsAsync(staff.VenueId, dto.HallIds);
            foreach (var hallId in hallIds)
                await _staffHallDal.AddAsync(new VenueStaffHall { VenueStaffId = staffId, HallId = hallId });

            return new SuccessResult(hallIds.Count == 0
                ? "Administrator sarayın bütün zallarında işləyə bilər."
                : $"{hallIds.Count} zal təyin olundu.");
        }

        // Administratorun səlahiyyətlərini təyin edir. Rəhbər «sorğuları görsün,
        // cavabı mən verim» deyə bilməlidir — əvvəl belə imkan yox idi.
        public async Task<IResult> SetPermissionsAsync(
            int staffId, SetStaffPermissionsDto dto, int ownerId)
        {
            var staff = await _staffDal.GetAsync(s => s.Id == staffId);
            if (staff is null)
                return new ErrorResult("Administrator tapılmadı.");

            if (!await _access.CanModifyAsync(staff.VenueId, ownerId))
                return new ErrorResult("Bu administrator sizin sarayınıza aid deyil.");

            staff.CanConfirmReservations = dto.CanConfirmReservations;
            staff.CanEditCalendar = dto.CanEditCalendar;
            staff.CanSendOffers = dto.CanSendOffers;

            await _staffDal.UpdateAsync(staff);

            var granted = new[]
            {
                dto.CanConfirmReservations ? "rezerv təsdiqi" : null,
                dto.CanEditCalendar ? "təqvim" : null,
                dto.CanSendOffers ? "təkliflər" : null
            }.Where(x => x is not null).ToList();

            return new SuccessResult(granted.Count == 0
                ? "Administrator yalnız sorğuları görəcək — cavabı siz verəcəksiniz."
                : $"Səlahiyyətlər yeniləndi: {string.Join(", ", granted)}.");
        }

        // Yalnız həmin saraya aid olan zal Id-lərini saxlayır — başqa sarayın zalı verilə bilməz
        private async Task<List<int>> FilterVenueHallsAsync(int venueId, List<int>? hallIds)
        {
            if (hallIds is null || hallIds.Count == 0)
                return new List<int>();

            var query = await _hallDal.GetListAsync(h => h.VenueId == venueId && hallIds.Contains(h.Id));
            return await query.Select(h => h.Id).ToListAsync();
        }

        // Təyinatı silir. Hesabın özü qalır, çünki onun təsdiqlədiyi rezervlər tarixçədədir
        public async Task<IResult> DeleteAsync(int staffId, int ownerId)
        {
            var staff = await _staffDal.GetAsync(s => s.Id == staffId);
            if (staff is null)
                return new ErrorResult("Administrator tapılmadı.");

            if (!await _access.CanModifyAsync(staff.VenueId, ownerId))
                return new ErrorResult("Bu administrator sizin sarayınıza aid deyil.");

            await _staffDal.DeleteAsync(staff);
            return new SuccessResult("Administrator siyahıdan silindi.");
        }

        // Deaktiv edilən administrator sistemə girə bilsə də sarayın datasını görmür
        public async Task<IResult> SetActiveAsync(int staffId, bool isActive, int ownerId)
        {
            var staff = await _staffDal.GetAsync(s => s.Id == staffId);
            if (staff is null)
                return new ErrorResult("Administrator tapılmadı.");

            if (!await _access.CanModifyAsync(staff.VenueId, ownerId))
                return new ErrorResult("Bu administrator sizin sarayınıza aid deyil.");

            staff.IsActive = isActive;
            await _staffDal.UpdateAsync(staff);

            return new SuccessResult(isActive ? "Administrator aktivləşdirildi." : "Administratorun girişi bağlandı.");
        }

        // Administrator parolunu unudubsa, sahib onu sıfırlaya bilər
        public async Task<IResult> ResetPasswordAsync(int staffId, ResetStaffPasswordDto dto, int ownerId)
        {
            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
                return new ErrorResult("Parol ən azı 6 simvol olmalıdır.");

            var staff = await _staffDal.GetAsync(s => s.Id == staffId);
            if (staff is null)
                return new ErrorResult("Administrator tapılmadı.");

            if (!await _access.CanModifyAsync(staff.VenueId, ownerId))
                return new ErrorResult("Bu administrator sizin sarayınıza aid deyil.");

            var user = await _userDal.GetAsync(u => u.Id == staff.UserId);
            if (user is null)
                return new ErrorResult("İstifadəçi tapılmadı.");

            HashingHelper.CreatePasswordHash(dto.NewPassword, out var hash, out var salt);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            await _userDal.UpdateAsync(user);

            return new SuccessResult("Parol yeniləndi.");
        }
    }
}
