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

        public VenueStaffManager(
            IVenueStaffDal staffDal,
            IVenueDal venueDal,
            IUserDal userDal,
            IVenueAccessService access)
        {
            _staffDal = staffDal;
            _venueDal = venueDal;
            _userDal = userDal;
            _access = access;
        }

        // Sahibin saraylarına təyin olunmuş bütün administratorlar
        public async Task<IDataResult<List<VenueStaffDto>>> GetMineAsync(int ownerId)
        {
            var venueQuery = await _venueDal.GetListAsync(v => v.OwnerId == ownerId);
            var venues = await venueQuery.Select(v => new { v.Id, v.Name }).ToListAsync();
            var venueIds = venues.Select(v => v.Id).ToList();

            var staffQuery = await _staffDal.GetListAsync(s => venueIds.Contains(s.VenueId));
            var staffList = await staffQuery.Include(s => s.User).ToListAsync();

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
                CreatedAt = s.CreatedAt
            }).ToList();

            return new SuccessDataResult<List<VenueStaffDto>>(result);
        }

        // Administrator hesabı yaradır. Saray mütləq sorğunu göndərənin öz sarayı olmalıdır —
        // əks halda bir sahib başqasının sarayına administrator təyin edə bilərdi.
        public async Task<IDataResult<VenueStaffDto>> CreateAsync(CreateVenueStaffDto dto, int ownerId)
        {
            if (!await _access.IsOwnerAsync(dto.VenueId, ownerId))
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

        // Deaktiv edilən administrator sistemə girə bilsə də sarayın datasını görmür
        public async Task<IResult> SetActiveAsync(int staffId, bool isActive, int ownerId)
        {
            var staff = await _staffDal.GetAsync(s => s.Id == staffId);
            if (staff is null)
                return new ErrorResult("Administrator tapılmadı.");

            if (!await _access.IsOwnerAsync(staff.VenueId, ownerId))
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

            if (!await _access.IsOwnerAsync(staff.VenueId, ownerId))
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
