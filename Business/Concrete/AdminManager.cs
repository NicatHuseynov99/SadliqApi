using Business.Abstract;
using Core.Utilities.Results;
using Core.Utilities.Security.Hashing;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos.Admin;
using Microsoft.EntityFrameworkCore;

namespace Business.Concrete
{
    public class AdminManager : IAdminService
    {
        private readonly IUserDal _userDal;
        private readonly IVenueDal _venueDal;
        private readonly IReservationDal _reservationDal;
        private readonly IReviewDal _reviewDal;
        private readonly IVenueStaffDal _venueStaffDal;
        private readonly IAuditContext _audit;

        public AdminManager(
            IUserDal userDal,
            IVenueDal venueDal,
            IReservationDal reservationDal,
            IReviewDal reviewDal,
            IVenueStaffDal venueStaffDal,
            IAuditContext audit)
        {
            _audit = audit;
            _userDal = userDal;
            _venueDal = venueDal;
            _reservationDal = reservationDal;
            _reviewDal = reviewDal;
            _venueStaffDal = venueStaffDal;
        }

        // Platformanın ümumi mənzərəsi — admin panelinin yuxarı hissəsi.
        // Dəstək admini üçün SuperAdmin hesabları saya daxil edilmir.
        public async Task<IDataResult<PlatformStatsDto>> GetStatsAsync(UserRole viewerRole)
        {
            var users = await _userDal.GetListAsync();
            users = HideSuperAdmins(users, viewerRole);

            var venues = await _venueDal.GetListAsync();
            var reservations = await _reservationDal.GetListAsync();
            var reviews = await _reviewDal.GetListAsync();

            var stats = new PlatformStatsDto
            {
                Users = await users.CountAsync(),
                Customers = await users.CountAsync(u => u.Role == UserRole.Customer),
                Owners = await users.CountAsync(u => u.Role == UserRole.Owner),
                BlockedUsers = await users.CountAsync(u => !u.IsActive),
                Venues = await venues.CountAsync(),
                PendingVenues = await venues.CountAsync(v => !v.IsApproved),
                Reservations = await reservations.CountAsync(),
                PendingReservations = await reservations.CountAsync(r => r.Status == ReservationStatus.Pending),
                Reviews = await reviews.CountAsync()
            };

            return new SuccessDataResult<PlatformStatsDto>(stats);
        }

        // İstifadəçi siyahısı: rola, ada/e-poçta/telefona və statusa görə süzülür.
        // Dəstək admini SuperAdmin hesablarını ümumiyyətlə görmür.
        public async Task<IDataResult<List<AdminUserDto>>> GetUsersAsync(string? role, string? q, string? status, UserRole viewerRole)
        {
            var query = await _userDal.GetListAsync();
            query = HideSuperAdmins(query, viewerRole);

            if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<UserRole>(role, true, out var parsed))
                query = query.Where(u => u.Role == parsed);

            if (status == "active")
                query = query.Where(u => u.IsActive);
            else if (status == "blocked")
                query = query.Where(u => !u.IsActive);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(u =>
                    u.FirstName.ToLower().Contains(term) ||
                    u.LastName.ToLower().Contains(term) ||
                    u.Email.ToLower().Contains(term) ||
                    u.Phone.Contains(term));
            }

            var users = await query.OrderBy(u => u.Id).ToListAsync();

            var venueQuery = await _venueDal.GetListAsync();
            var venueCounts = await venueQuery
                .GroupBy(v => v.OwnerId)
                .Select(g => new { OwnerId = g.Key, Count = g.Count() })
                .ToListAsync();

            var result = users
                .Select(u => ToDto(u, venueCounts.FirstOrDefault(c => c.OwnerId == u.Id)?.Count ?? 0))
                .ToList();

            return new SuccessDataResult<List<AdminUserDto>>(result);
        }

        // Bir istifadəçinin tam mənzərəsi: fəaliyyəti və bağlı olduğu saraylar
        public async Task<IDataResult<AdminUserDetailDto>> GetUserDetailAsync(int userId, UserRole viewerRole)
        {
            var user = await FindVisibleUserAsync(userId, viewerRole);
            if (user is null)
                return new ErrorDataResult<AdminUserDetailDto>("İstifadəçi tapılmadı.");

            var venueQuery = await _venueDal.GetListAsync();
            var ownedVenues = await venueQuery.Where(v => v.OwnerId == user.Id)
                .Select(v => v.Name).ToListAsync();

            // Saray administratorudursa, təyin olunduğu sarayları da göstəririk
            var staffQuery = await _venueStaffDal.GetListAsync();
            var staffVenueIds = await staffQuery.Where(s => s.UserId == user.Id)
                .Select(s => s.VenueId).ToListAsync();
            var staffVenues = await venueQuery.Where(v => staffVenueIds.Contains(v.Id))
                .Select(v => v.Name).ToListAsync();

            var reservations = await _reservationDal.GetListAsync();
            var myReservations = reservations.Where(r => r.CustomerId == user.Id);

            var reviews = await _reviewDal.GetListAsync();

            var detail = new AdminUserDetailDto
            {
                User = ToDto(user, ownedVenues.Count),
                ReservationCount = await myReservations.CountAsync(),
                PendingReservationCount = await myReservations.CountAsync(r => r.Status == ReservationStatus.Pending),
                ReviewCount = await reviews.CountAsync(r => r.CustomerId == user.Id),
                LastReservationAt = await myReservations
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => (DateTime?)r.CreatedAt)
                    .FirstOrDefaultAsync(),
                Venues = ownedVenues.Concat(staffVenues).Distinct().ToList()
            };

            return new SuccessDataResult<AdminUserDetailDto>(detail);
        }

        // Rol dəyişikliyi. Admin öz rolunu aşağı sala bilməz — sistemdə adminsiz qalma riski var
        public async Task<IResult> ChangeRoleAsync(int userId, ChangeRoleDto dto, int currentAdminId, UserRole viewerRole)
        {
            if (!Enum.TryParse<UserRole>(dto.Role, true, out var role))
                return new ErrorResult("Belə rol yoxdur.");

            if (userId == currentAdminId)
                return new ErrorResult("Öz rolunuzu dəyişə bilməzsiniz.");

            var user = await FindVisibleUserAsync(userId, viewerRole);
            if (user is null)
                return new ErrorResult("İstifadəçi tapılmadı.");

            // Dəstək admini kimisə SuperAdmin edə bilməz
            if (viewerRole != UserRole.SuperAdmin && role == UserRole.SuperAdmin)
                return new ErrorResult("SuperAdmin rolunu yalnız platforma sahibi verə bilər.");

            var oldRole = user.Role;
            user.Role = role;
            await _userDal.UpdateAsync(user);

            _audit.Set("UserRoleChanged",
                $"{user.FullName} istifadəçisinin rolunu «{RoleName(oldRole)}» → «{RoleName(role)}» dəyişdi",
                "User", user.Id);

            return new SuccessResult($"{user.FullName} üçün rol dəyişdirildi: {RoleName(role)}");
        }

        // Hesabın bloklanması / açılması. Məlumat silinmir, sadəcə giriş bağlanır.
        public async Task<IResult> SetUserActiveAsync(int userId, SetUserActiveDto dto, int currentAdminId, UserRole viewerRole)
        {
            if (userId == currentAdminId)
                return new ErrorResult("Öz hesabınızı bloklaya bilməzsiniz.");

            var user = await FindVisibleUserAsync(userId, viewerRole);
            if (user is null)
                return new ErrorResult("İstifadəçi tapılmadı.");

            if (user.IsActive == dto.IsActive)
                return new SuccessResult("Hesabın statusu artıq belədir.");

            user.IsActive = dto.IsActive;
            await _userDal.UpdateAsync(user);

            _audit.Set(dto.IsActive ? "UserUnblocked" : "UserBlocked",
                dto.IsActive
                    ? $"{user.FullName} hesabının bloku götürüldü"
                    : $"{user.FullName} hesabı bloklandı",
                "User", user.Id);

            return new SuccessResult(dto.IsActive
                ? $"{user.FullName} hesabı yenidən aktivdir."
                : $"{user.FullName} hesabı bloklandı.");
        }

        // Admin istifadəçiyə yeni parol təyin edir (dəstək zəngləri üçün)
        public async Task<IResult> ResetUserPasswordAsync(int userId, AdminResetPasswordDto dto, UserRole viewerRole)
        {
            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Trim().Length < 6)
                return new ErrorResult("Parol ən azı 6 simvol olmalıdır.");

            var user = await FindVisibleUserAsync(userId, viewerRole);
            if (user is null)
                return new ErrorResult("İstifadəçi tapılmadı.");

            HashingHelper.CreatePasswordHash(dto.NewPassword.Trim(), out var hash, out var salt);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            await _userDal.UpdateAsync(user);

            _audit.Set("UserPasswordReset",
                $"{user.FullName} istifadəçisinin parolu admin tərəfindən sıfırlandı",
                "User", user.Id);

            return new SuccessResult($"{user.FullName} üçün yeni parol təyin edildi.");
        }

        // Dəstək admini üçün SuperAdmin hesabları siyahıdan çıxarılır
        private static IQueryable<User> HideSuperAdmins(IQueryable<User> query, UserRole viewerRole) =>
            viewerRole == UserRole.SuperAdmin
                ? query
                : query.Where(u => u.Role != UserRole.SuperAdmin);

        // Baxan istifadəçinin görmək hüququ olan hesabı tapır.
        // Dəstək admini üçün SuperAdmin sadəcə "tapılmadı" kimi görünür.
        private async Task<User?> FindVisibleUserAsync(int userId, UserRole viewerRole)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user is null) return null;
            if (viewerRole != UserRole.SuperAdmin && user.Role == UserRole.SuperAdmin) return null;
            return user;
        }

        private static AdminUserDto ToDto(User u, int venueCount) => new()
        {
            Id = u.Id,
            FirstName = u.FirstName,
            LastName = u.LastName,
            FullName = u.FullName,
            Email = u.Email,
            Phone = u.Phone,
            Role = u.Role.ToString(),
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt,
            VenueCount = venueCount
        };

        // Rolların Azərbaycanca adları — jurnal mətnlərində işlənir
        private static string RoleName(UserRole role) => role switch
        {
            UserRole.Customer => "Müştəri",
            UserRole.Owner => "Saray rəhbərliyi",
            UserRole.VenueAdmin => "Saray administratoru",
            UserRole.Support => "Dəstək admini",
            UserRole.SuperAdmin => "Platforma sahibi",
            _ => role.ToString()
        };
    }
}
