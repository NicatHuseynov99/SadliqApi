using Business.Abstract;
using Core.Utilities.Results;
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
        private readonly IAuditContext _audit;

        public AdminManager(
            IUserDal userDal,
            IVenueDal venueDal,
            IReservationDal reservationDal,
            IReviewDal reviewDal,
            IAuditContext audit)
        {
            _audit = audit;
            _userDal = userDal;
            _venueDal = venueDal;
            _reservationDal = reservationDal;
            _reviewDal = reviewDal;
        }

        // Platformanın ümumi mənzərəsi — admin panelinin yuxarı hissəsi
        public async Task<IDataResult<PlatformStatsDto>> GetStatsAsync()
        {
            var users = await _userDal.GetListAsync();
            var venues = await _venueDal.GetListAsync();
            var reservations = await _reservationDal.GetListAsync();
            var reviews = await _reviewDal.GetListAsync();

            var stats = new PlatformStatsDto
            {
                Users = await users.CountAsync(),
                Customers = await users.CountAsync(u => u.Role == UserRole.Customer),
                Owners = await users.CountAsync(u => u.Role == UserRole.Owner),
                Venues = await venues.CountAsync(),
                PendingVenues = await venues.CountAsync(v => !v.IsApproved),
                Reservations = await reservations.CountAsync(),
                PendingReservations = await reservations.CountAsync(r => r.Status == ReservationStatus.Pending),
                Reviews = await reviews.CountAsync()
            };

            return new SuccessDataResult<PlatformStatsDto>(stats);
        }

        // İstifadəçi siyahısı, istəyə görə rola süzülmüş
        public async Task<IDataResult<List<AdminUserDto>>> GetUsersAsync(string? role)
        {
            var query = await _userDal.GetListAsync();

            if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<UserRole>(role, true, out var parsed))
                query = query.Where(u => u.Role == parsed);

            var users = await query.OrderBy(u => u.Id).ToListAsync();

            var venueQuery = await _venueDal.GetListAsync();
            var venueCounts = await venueQuery
                .GroupBy(v => v.OwnerId)
                .Select(g => new { OwnerId = g.Key, Count = g.Count() })
                .ToListAsync();

            var result = users.Select(u => new AdminUserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.Phone,
                Role = u.Role.ToString(),
                CreatedAt = u.CreatedAt,
                VenueCount = venueCounts.FirstOrDefault(c => c.OwnerId == u.Id)?.Count ?? 0
            }).ToList();

            return new SuccessDataResult<List<AdminUserDto>>(result);
        }

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

        // Rol dəyişikliyi. Admin öz rolunu aşağı sala bilməz — sistemdə adminsiz qalma riski var
        public async Task<IResult> ChangeRoleAsync(int userId, ChangeRoleDto dto, int currentAdminId)
        {
            if (!Enum.TryParse<UserRole>(dto.Role, true, out var role))
                return new ErrorResult("Belə rol yoxdur.");

            if (userId == currentAdminId)
                return new ErrorResult("Öz rolunuzu dəyişə bilməzsiniz.");

            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user is null)
                return new ErrorResult("İstifadəçi tapılmadı.");

            var oldRole = user.Role;
            user.Role = role;
            await _userDal.UpdateAsync(user);

            _audit.Set("UserRoleChanged",
                $"{user.FullName} istifadəçisinin rolunu «{RoleName(oldRole)}» → «{RoleName(role)}» dəyişdi",
                "User", user.Id);

            return new SuccessResult($"{user.FullName} üçün rol dəyişdirildi: {role}");
        }
    }
}
