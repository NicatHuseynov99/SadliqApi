using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;
using Entities.Dtos.Owner;
using Microsoft.EntityFrameworkCore;

namespace Business.Concrete
{
    // Sahibkar üçün hesabat. Məlumat onsuz da yığılırdı — sadəcə sahibə
    // göstərilmirdi. Sahibi platformada saxlayan şey budur: sarayına neçə
    // nəfər baxdı, neçəsi sorğu göndərdi, nəyi düzəltsə daha çox olar.
    public class OwnerStatsManager : IOwnerStatsService
    {
        private readonly SadliqDbContext _context;
        private readonly IVenueAccessService _access;

        public OwnerStatsManager(SadliqDbContext context, IVenueAccessService access)
        {
            _context = context;
            _access = access;
        }

        public async Task<IDataResult<List<OwnerStatsDto>>> GetAsync(int ownerId, int days, int? venueId)
        {
            var period = Math.Clamp(days, 7, 365);

            var venueIds = await _access.GetManagedVenueIdsAsync(ownerId);

            if (venueId is int single)
            {
                if (!venueIds.Contains(single))
                    return new ErrorDataResult<List<OwnerStatsDto>>("Bu saray sizə aid deyil.");

                venueIds = new List<int> { single };
            }

            if (venueIds.Count == 0)
                return new SuccessDataResult<List<OwnerStatsDto>>(new List<OwnerStatsDto>());

            var since = DateTime.UtcNow.AddDays(-period);
            var previousSince = since.AddDays(-period);
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

            var venues = await _context.Venues
                .Where(v => venueIds.Contains(v.Id))
                .Include(v => v.Images)
                .Include(v => v.Halls)
                .Include(v => v.Menus)
                .ToListAsync();

            var list = new List<OwnerStatsDto>();

            foreach (var venue in venues)
            {
                var views = _context.PageViews.Where(p => p.VenueId == venue.Id && p.CreatedAt >= since);

                var previousViews = await _context.PageViews
                    .CountAsync(p => p.VenueId == venue.Id
                                     && p.CreatedAt >= previousSince
                                     && p.CreatedAt < since);

                var viewCount = await views.CountAsync();

                var reservations = await _context.Reservations
                    .Where(r => r.VenueId == venue.Id && r.CreatedAt >= since)
                    .Include(r => r.Menu)
                    .ToListAsync();

                var confirmed = reservations.Where(r => r.Status == ReservationStatus.Confirmed).ToList();
                var expired = reservations.Where(r => r.Status == ReservationStatus.Expired).ToList();

                // Cavablanmış sorğularda sorğudan cavaba qədər keçən vaxt
                var answered = reservations
                    .Where(r => r.RespondedAt.HasValue)
                    .Select(r => (r.RespondedAt!.Value - r.CreatedAt).TotalHours)
                    .ToList();

                var reviews = await _context.Reviews
                    .Where(r => r.VenueId == venue.Id)
                    .Select(r => (double)r.Rating)
                    .ToListAsync();

                var stats = new OwnerStatsDto
                {
                    VenueId = venue.Id,
                    VenueName = venue.Name,
                    Days = period,

                    Views = viewCount,
                    UniqueVisitors = await views.Select(p => p.SessionId).Distinct().CountAsync(),
                    ViewsChangePercent = previousViews == 0
                        ? (viewCount > 0 ? 100 : 0)
                        : (int)Math.Round((viewCount - previousViews) * 100.0 / previousViews),

                    Favorites = await _context.Favorites.CountAsync(f => f.VenueId == venue.Id),

                    Requests = reservations.Count,
                    Confirmed = confirmed.Count,
                    Rejected = reservations.Count(r => r.Status == ReservationStatus.Rejected),
                    Expired = expired.Count,
                    Pending = reservations.Count(r =>
                        r.Status is ReservationStatus.Pending or ReservationStatus.CounterOffered),

                    ViewToRequestPercent = viewCount == 0
                        ? 0
                        : (int)Math.Round(reservations.Count * 100.0 / viewCount),

                    ConfirmPercent = reservations.Count == 0
                        ? 0
                        : (int)Math.Round(confirmed.Count * 100.0 / reservations.Count),

                    AverageResponseHours = answered.Count == 0
                        ? null
                        : Math.Round(answered.Average(), 1),

                    // Cavabsız qalıb bağlanan sorğuların dəyəri
                    LostIncome = expired.Sum(r => (r.Menu?.PricePerPerson ?? 0) * r.GuestCount),

                    ExpectedIncome = confirmed
                        .Where(r => r.EventDate >= today)
                        .Sum(r => (r.Menu?.PricePerPerson ?? 0) * r.GuestCount),

                    Rating = reviews.Count == 0 ? null : Math.Round(reviews.Average(), 1),
                    ReviewCount = reviews.Count,

                    OffersSent = await _context.VenueOffers.CountAsync(o => o.VenueId == venue.Id),
                    OffersAccepted = await _context.VenueOffers
                        .CountAsync(o => o.VenueId == venue.Id && o.Status == VenueOfferStatus.Accepted)
                };

                stats.Advice = BuildAdvice(venue, stats);
                list.Add(stats);
            }

            return new SuccessDataResult<List<OwnerStatsDto>>(
                list.OrderByDescending(s => s.Views).ToList());
        }

        // Rəqəmi göstərmək azdır — nə etmək lazım olduğunu demək lazımdır
        private static List<string> BuildAdvice(Venue venue, OwnerStatsDto stats)
        {
            var advice = new List<string>();

            if (string.IsNullOrWhiteSpace(venue.CoverImageUrl))
                advice.Add("Fon şəkli yoxdur — şəkilsiz saraya baxış kəskin az olur.");

            if (venue.Images.Count < 5)
                advice.Add($"Cəmi {venue.Images.Count} şəkil var. 5-dən çox şəkil qoyan saraylara baxış daha uzun olur.");

            if (string.IsNullOrWhiteSpace(venue.VideoUrl))
                advice.Add("Video əlavə edin — video şəkildən güclü təsir bağışlayır.");

            if (venue.Latitude is null)
                advice.Add("Xəritədə yeriniz göstərilməyib — xəritə axtarışında görünmürsünüz.");

            if ((venue.ParkingCapacity ?? 0) == 0)
                advice.Add("Parkinq tutumu yazılmayıb — «parkinqi olan» süzgəcinə düşmürsünüz.");

            if (venue.Menus.Count == 0)
                advice.Add("Menyu əlavə edilməyib — müştəri qiyməti görmədən sorğu göndərmir.");

            if (stats.AverageResponseHours > 24)
                advice.Add($"Sorğulara orta cavab müddətiniz {stats.AverageResponseHours} saatdır. " +
                           "Tez cavab verən saraylar daha çox rezerv alır.");

            if (stats.Expired > 0)
                advice.Add($"{stats.Expired} sorğu cavabsız qalıb bağlanıb — təxminən " +
                           $"{stats.LostIncome:N0} ₼ itirilib.");

            if (stats.Requests > 0 && stats.ConfirmPercent < 40)
                advice.Add("Sorğuların yarısından çoxunu qəbul etmirsiniz. " +
                           "«Rədd et» əvəzinə başqa tarix təklif etməyi yoxlayın.");

            if (stats.ReviewCount == 0)
                advice.Add("Hələ rəyiniz yoxdur. Keçmiş qonaqlarınıza rəy yazmağı xatırladın.");

            return advice;
        }
    }
}
