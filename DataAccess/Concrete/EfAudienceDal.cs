using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;
using Entities.Dtos.Notification;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Concrete
{
    // Elanın kimə gedəcəyini hesablayır. Şərtlər bir-birinin üstünə yığılır:
    // rol → şəhər → saray → davranış. Nəticə yalnız istifadəçi nömrələridir.
    public class EfAudienceDal : IAudienceDal
    {
        private readonly SadliqDbContext _context;

        public EfAudienceDal(SadliqDbContext context)
        {
            _context = context;
        }

        public async Task<List<int>> ResolveAsync(AudienceDto audience)
        {
            // Sınaq göndərişi: qalan şərtlərə baxılmır
            if (audience.UserIds.Count > 0)
                return await _context.Users
                    .Where(u => audience.UserIds.Contains(u.Id) && u.IsActive)
                    .Select(u => u.Id)
                    .ToListAsync();

            // Bloklanmış hesaba bildiriş getmir
            var query = _context.Users.Where(u => u.IsActive);

            if (audience.Roles.Count > 0)
                query = query.Where(u => audience.Roles.Contains(u.Role));

            // Şəhər istifadəçidə saxlanılmır — onu rezerv etdiyi və ya
            // sahibi olduğu sarayın şəhəri müəyyən edir
            if (audience.Cities.Count > 0)
            {
                var cityUsers = _context.Reservations
                    .Where(r => audience.Cities.Contains(r.Venue!.City))
                    .Select(r => r.CustomerId)
                    .Union(_context.Venues
                        .Where(v => audience.Cities.Contains(v.City))
                        .Select(v => v.OwnerId));

                query = query.Where(u => cityUsers.Contains(u.Id));
            }

            // Konkret sarayın qonaqları
            if (audience.VenueId is int venueId)
            {
                var venueGuests = _context.Reservations
                    .Where(r => r.VenueId == venueId)
                    .Select(r => r.CustomerId);

                query = query.Where(u => venueGuests.Contains(u.Id));
            }

            query = ApplyBehavior(query, audience);

            return await query.Select(u => u.Id).ToListAsync();
        }

        private IQueryable<User> ApplyBehavior(IQueryable<User> query, AudienceDto audience)
        {
            var since = DateTime.UtcNow.AddDays(-Math.Max(1, audience.BehaviorDays));

            switch (audience.Behavior)
            {
                case AudienceBehavior.ReservedRecently:
                    var recent = _context.Reservations
                        .Where(r => r.CreatedAt >= since)
                        .Select(r => r.CustomerId);
                    return query.Where(u => recent.Contains(u.Id));

                case AudienceBehavior.NeverReserved:
                    var everReserved = _context.Reservations.Select(r => r.CustomerId);
                    return query.Where(u => !everReserved.Contains(u.Id));

                case AudienceBehavior.FavoritedNotReserved:
                    // Ən dəyərli auditoriya: maraqlanıb, amma addım atmayıb
                    var favorited = _context.Favorites.Select(f => f.UserId);
                    var reserved = _context.Reservations.Select(r => r.CustomerId);
                    return query.Where(u => favorited.Contains(u.Id) && !reserved.Contains(u.Id));

                case AudienceBehavior.ReservationRejected:
                    var rejected = _context.Reservations
                        .Where(r => r.CreatedAt >= since
                                    && (r.Status == ReservationStatus.Rejected
                                        || r.Status == ReservationStatus.Expired))
                        .Select(r => r.CustomerId);
                    return query.Where(u => rejected.Contains(u.Id));

                case AudienceBehavior.Inactive:
                    // Sonuncu hərəkət kimi rezerv və ziyarət qeydlərinə baxılır
                    var recentlyActive = _context.Reservations
                        .Where(r => r.CreatedAt >= since)
                        .Select(r => r.CustomerId);

                    var visited = _context.PageViews
                        .Where(p => p.CreatedAt >= since && p.UserId != null)
                        .Select(p => p.UserId!.Value);

                    return query.Where(u => !recentlyActive.Contains(u.Id) && !visited.Contains(u.Id));

                default:
                    return query;
            }
        }

        public async Task<List<string>> GetSampleNamesAsync(List<int> userIds, int take)
        {
            return await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .OrderBy(u => u.Id)
                .Take(take)
                .Select(u => u.FirstName + " " + u.LastName + " · " + u.Email)
                .ToListAsync();
        }
    }
}
