using Business.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace SadliqApi.Reservations
{
    // Cavabsız qalan rezerv sorğusu tarixi əbədi bağlaya bilməz.
    // Bu xidmət saatda bir dəfə işə düşür və iki iş görür:
    //   1) müddəti bitmiş tutulmaları boşaldır, sorğunu «vaxtı keçdi» edir;
    //   2) müddətin yarısı keçəndə sahibə bir dəfəlik xatırlatma göndərir.
    public class ReservationHoldService : BackgroundService
    {
        private const int DefaultHoldHours = 48;

        // Tədbirə neçə gün qalmış xatırladılsın və neçə gün sonra rəy istənilsin
        private const int EventReminderDays = 3;
        private const int ReviewInviteAfterDays = 2;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReservationHoldService> _logger;

        public ReservationHoldService(IServiceScopeFactory scopeFactory, ILogger<ReservationHoldService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<SadliqDbContext>();
                    var settings = scope.ServiceProvider.GetRequiredService<IPlatformSettingsService>();
                    var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

                    var holdHours = await settings.GetIntAsync("reservation_hold_hours", DefaultHoldHours);

                    await ReleaseExpiredAsync(context, notifications, stoppingToken);
                    await RemindOwnersAsync(context, notifications, holdHours, stoppingToken);

                    await RemindUpcomingEventsAsync(context, notifications, stoppingToken);
                    await InviteReviewsAsync(context, notifications, stoppingToken);

                    // Tərs axtarışın müddəti bitmiş sorğuları da burada bağlanır
                    var requests = scope.ServiceProvider.GetRequiredService<IVenueRequestService>();
                    var closed = await requests.ExpireOldAsync(stoppingToken);

                    if (closed > 0)
                        _logger.LogInformation("{Count} müddəti bitmiş sorğu bağlandı.", closed);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Rezerv tutulmaları yoxlanıla bilmədi.");
                }

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        // Müddəti bitmiş tutulmalar
        private async Task ReleaseExpiredAsync(SadliqDbContext context, INotificationService notifications, CancellationToken token)
        {
            var now = DateTime.UtcNow;

            var expired = await context.HallAvailabilities
                .Where(a => a.Status == AvailabilityStatus.Held
                            && a.HoldExpiresAt != null
                            && a.HoldExpiresAt < now)
                .ToListAsync(token);

            if (expired.Count == 0) return;

            var closed = new List<Reservation>();

            foreach (var slot in expired)
            {
                slot.Status = AvailabilityStatus.Available;
                slot.HoldExpiresAt = null;

                // Həmin gün üçün cavabsız qalan sorğular bağlanır
                var pending = await context.Reservations
                    .Where(r => r.HallId == slot.HallId
                                && r.EventDate == slot.Date
                                && r.Status == ReservationStatus.Pending)
                    .ToListAsync(token);

                foreach (var reservation in pending)
                {
                    reservation.Status = ReservationStatus.Expired;
                    closed.Add(reservation);
                }
            }

            await context.SaveChangesAsync(token);

            // Bildirişlər bazaya yazıldıqdan sonra göndərilir ki, xəta olsa
            // status dəyişikliyi itməsin
            foreach (var reservation in closed)
            {
                var venueName = await context.Venues
                    .Where(v => v.Id == reservation.VenueId)
                    .Select(v => v.Name)
                    .FirstOrDefaultAsync(token) ?? "Saray";

                await notifications.NotifyAsync(
                    reservation.CustomerId,
                    new NotificationMessage(
                        NotificationType.ReservationExpired,
                        "Rezerv sorğunuz bağlandı",
                        $"«{venueName}» üçün {reservation.EventDate:dd.MM.yyyy} tarixinə göndərdiyiniz " +
                        "sorğuya vaxtında cavab verilmədi. Sarayla birbaşa əlaqə saxlaya və ya " +
                        "başqa saray seçə bilərsiniz.",
                        "/rezervlerim"));
            }

            _logger.LogInformation("{Count} vaxtı keçmiş tutulma boşaldıldı.", expired.Count);
        }

        // Müddətin yarısı keçib, sahib hələ cavab verməyibsə — bir dəfə xatırlat
        private async Task RemindOwnersAsync(
            SadliqDbContext context, INotificationService notifications, int holdHours, CancellationToken token)
        {
            var threshold = DateTime.UtcNow.AddHours(-holdHours / 2.0);

            var waiting = await context.Reservations
                .Where(r => r.Status == ReservationStatus.Pending
                            && r.OwnerRemindedAt == null
                            && r.CreatedAt < threshold)
                .Include(r => r.Venue)
                .Include(r => r.Hall)
                .Take(100)
                .ToListAsync(token);

            if (waiting.Count == 0) return;

            foreach (var reservation in waiting)
                reservation.OwnerRemindedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(token);

            foreach (var reservation in waiting)
            {
                if (reservation.Venue is null) continue;

                await notifications.NotifyAsync(
                    reservation.Venue.OwnerId,
                    new NotificationMessage(
                        NotificationType.ReservationReminder,
                        "Cavab gözləyən sorğu var",
                        $"«{reservation.Venue.Name}» — «{reservation.Hall?.Name}», " +
                        $"{reservation.EventDate:dd.MM.yyyy} tarixi üçün sorğu hələ cavabsızdır. " +
                        "Cavab verilməsə, tarix avtomatik boşalacaq və sorğu bağlanacaq.",
                        "/panel"));
            }
        }

        // Tədbirə 3 gün qalmış hər iki tərəfə xatırlatma.
        // Toy bir dəfə olur — unudulmaması platformanın işidir.
        private async Task RemindUpcomingEventsAsync(
            SadliqDbContext context, INotificationService notifications, CancellationToken token)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            var soon = today.AddDays(EventReminderDays);

            var upcoming = await context.Reservations
                .Where(r => r.Status == ReservationStatus.Confirmed
                            && r.EventReminderSentAt == null
                            && r.EventDate > today
                            && r.EventDate <= soon)
                .Include(r => r.Venue)
                .Include(r => r.Hall)
                .Take(200)
                .ToListAsync(token);

            if (upcoming.Count == 0) return;

            foreach (var reservation in upcoming)
                reservation.EventReminderSentAt = DateTime.UtcNow;

            await context.SaveChangesAsync(token);

            foreach (var reservation in upcoming)
            {
                var days = reservation.EventDate.DayNumber - today.DayNumber;
                var venueName = reservation.Venue?.Name ?? "Saray";

                await notifications.NotifyAsync(
                    reservation.CustomerId,
                    new NotificationMessage(
                        NotificationType.ReservationReminder,
                        $"Tədbirinizə {days} gün qaldı",
                        $"«{venueName}» — {reservation.Hall?.Name}, {reservation.EventDate:dd.MM.yyyy}, " +
                        $"{reservation.GuestCount} qonaq.\n" +
                        "Qonaq sayında dəyişiklik varsa sarayı əvvəlcədən xəbərdar edin.",
                        "/rezervlerim"));

                if (reservation.Venue is not null)
                    await notifications.NotifyAsync(
                        reservation.Venue.OwnerId,
                        new NotificationMessage(
                            NotificationType.ReservationReminder,
                            $"{days} gün sonra tədbir var",
                            $"«{reservation.Hall?.Name}» — {reservation.EventDate:dd.MM.yyyy}, " +
                            $"{reservation.GuestCount} qonaq. Əlaqə: {reservation.ContactPhone}",
                            "/panel"));
            }
        }

        // Tədbirdən 2 gün sonra rəy dəvəti. Rəy yalnız keçmiş qonaqdan qəbul
        // olunduğu üçün ən dəqiq rəylər məhz bu dəvətdən gəlir.
        private async Task InviteReviewsAsync(
            SadliqDbContext context, INotificationService notifications, CancellationToken token)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            var since = today.AddDays(-ReviewInviteAfterDays);

            var finished = await context.Reservations
                .Where(r => r.Status == ReservationStatus.Confirmed
                            && r.ReviewInviteSentAt == null
                            && r.EventDate <= since
                            && r.EventDate > since.AddDays(-14))
                .Include(r => r.Venue)
                .Take(200)
                .ToListAsync(token);

            if (finished.Count == 0) return;

            foreach (var reservation in finished)
                reservation.ReviewInviteSentAt = DateTime.UtcNow;

            await context.SaveChangesAsync(token);

            foreach (var reservation in finished)
            {
                // Artıq rəy yazıbsa dəvət göndərilmir
                var hasReview = await context.Reviews
                    .IgnoreQueryFilters()
                    .AnyAsync(r => r.ReservationId == reservation.Id, token);

                if (hasReview) continue;

                await notifications.NotifyAsync(
                    reservation.CustomerId,
                    new NotificationMessage(
                        NotificationType.SystemMessage,
                        "Tədbiriniz necə keçdi?",
                        $"«{reservation.Venue?.Name ?? "Saray"}» haqqında rəyiniz başqa ailələrə " +
                        "seçim etməkdə kömək edir. Bir dəqiqənizi alır.",
                        $"/venue/{reservation.VenueId}"));
            }
        }
    }
}
