using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;
using Entities.Dtos.VenueRequest;
using Microsoft.EntityFrameworkCore;

namespace Business.Concrete
{
    // Birbaşa rezerv üzrə yazışma. Təklif söhbətinin məntiqi təkrarlanır,
    // amma ona toxunulmur: işləyən axını dəyişmək əvəzinə yanına qoyulur.
    public class ReservationChatManager : IReservationChatService
    {
        private const int MaxBodyLength = 1000;

        private readonly SadliqDbContext _context;
        private readonly IReservationDal _reservationDal;
        private readonly IVenueDal _venueDal;
        private readonly IUserDal _userDal;
        private readonly IVenueAccessService _access;
        private readonly INotificationService _notifications;

        public ReservationChatManager(
            SadliqDbContext context,
            IReservationDal reservationDal,
            IVenueDal venueDal,
            IUserDal userDal,
            IVenueAccessService access,
            INotificationService notifications)
        {
            _context = context;
            _reservationDal = reservationDal;
            _venueDal = venueDal;
            _userDal = userDal;
            _access = access;
            _notifications = notifications;
        }

        public async Task<IDataResult<List<OfferMessageDto>>> GetMessagesAsync(
            int reservationId, int userId)
        {
            var (reservation, isCustomer, error) = await LoadChatAsync(reservationId, userId);
            if (error is not null)
                return new ErrorDataResult<List<OfferMessageDto>>(error.Message);

            // Admin tərəfindən gizlədilmiş mesaj tərəflərə göstərilmir
            var messages = await _context.OfferMessages
                .Where(m => m.ReservationId == reservationId && !m.IsDeleted && !m.IsHidden)
                .Include(m => m.Sender)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            // Qarşı tərəfin mesajları açılan kimi oxunmuş sayılır
            var unread = messages
                .Where(m => m.IsFromCustomer != isCustomer && m.ReadAt is null)
                .ToList();

            if (unread.Count > 0)
            {
                var now = DateTime.UtcNow;
                foreach (var message in unread) message.ReadAt = now;
                await _context.SaveChangesAsync();
            }

            var list = messages.Select(m => new OfferMessageDto
            {
                Id = m.Id,
                IsFromCustomer = m.IsFromCustomer,
                SenderName = m.Sender is null
                    ? "—"
                    : $"{m.Sender.FirstName} {m.Sender.LastName}".Trim(),
                Body = m.Body,
                CreatedAt = m.CreatedAt,
                IsRead = m.ReadAt is not null
            }).ToList();

            return new SuccessDataResult<List<OfferMessageDto>>(list);
        }

        public async Task<IDataResult<OfferMessageDto>> SendMessageAsync(
            int reservationId, string body, int userId)
        {
            if (string.IsNullOrWhiteSpace(body))
                return new ErrorDataResult<OfferMessageDto>("Mesaj boş ola bilməz.");

            body = body.Trim();

            if (body.Length > MaxBodyLength)
                return new ErrorDataResult<OfferMessageDto>(
                    $"Mesaj {MaxBodyLength} simvoldan uzun ola bilməz.");

            var (reservation, isCustomer, error) = await LoadChatAsync(reservationId, userId);
            if (error is not null)
                return new ErrorDataResult<OfferMessageDto>(error.Message);

            // Bağlanmış rezervdə yazışma oxunur, amma davam etdirilmir: söhbətin
            // özü dəlil kimi qalmalıdır, üstünə yeni mesaj yazılmamalıdır.
            if (reservation!.Status is ReservationStatus.Rejected
                or ReservationStatus.Cancelled
                or ReservationStatus.Expired)
                return new ErrorDataResult<OfferMessageDto>(
                    "Bu rezerv bağlanıb — yazışma yalnız oxumaq üçün açıqdır.");

            var sender = await _userDal.GetAsync(u => u.Id == userId);

            var message = new OfferMessage
            {
                ReservationId = reservationId,
                SenderUserId = userId,
                IsFromCustomer = isCustomer,
                Body = body,
                CreatedAt = DateTime.UtcNow
            };

            _context.OfferMessages.Add(message);
            await _context.SaveChangesAsync();

            await NotifyCounterpartAsync(reservation, isCustomer, sender, body);

            return new SuccessDataResult<OfferMessageDto>(
                new OfferMessageDto
                {
                    Id = message.Id,
                    IsFromCustomer = message.IsFromCustomer,
                    SenderName = sender is null
                        ? "—"
                        : $"{sender.FirstName} {sender.LastName}".Trim(),
                    Body = message.Body,
                    CreatedAt = message.CreatedAt,
                    IsRead = false
                },
                "Mesaj göndərildi.");
        }

        // Qarşı tərəfə bildiriş: mesajın əvvəli də görünsün ki, açmağa dəyərmi bilsin
        private async Task NotifyCounterpartAsync(
            Reservation reservation, bool isCustomer, User? sender, string body)
        {
            var preview = body.Length > 120 ? body[..120] + "…" : body;
            var venue = await _venueDal.GetAsync(v => v.Id == reservation.VenueId);

            if (isCustomer)
            {
                if (venue is null) return;

                var recipients = await _access.GetNotifyUserIdsAsync(
                    venue.Id, venue.OwnerId, StaffPermission.ConfirmReservations);

                await _notifications.NotifyManyAsync(
                    recipients,
                    new NotificationMessage(
                        NotificationType.SystemMessage,
                        $"Yeni mesaj — {sender?.FullName ?? "müştəri"}",
                        $"«{venue.Name}» üzrə rezerv yazışması:\n{preview}",
                        "/panel"),
                    NotificationChannels.InApp);

                return;
            }

            await _notifications.NotifyAsync(
                reservation.CustomerId,
                new NotificationMessage(
                    NotificationType.SystemMessage,
                    $"Yeni mesaj — {venue?.Name ?? "saray"}",
                    preview,
                    "/rezervlerim"));
        }

        // Yazışmaya kim çıxa bilər: rezervin müştərisi və sarayı idarə edənlər.
        //
        // Söhbət rezerv cavab gözləyəndə də açıqdır — «uşaq stulu varmı» sualı
        // təsdiqdən əvvəl verilir və elə cavaba təsir edir.
        private async Task<(Reservation? Reservation, bool IsCustomer, IResult? Error)> LoadChatAsync(
            int reservationId, int userId)
        {
            var reservation = await _reservationDal.GetAsync(r => r.Id == reservationId);
            if (reservation is null)
                return (null, false, new ErrorResult("Rezerv tapılmadı."));

            if (reservation.CustomerId == userId)
                return (reservation, true, null);

            if (await _access.CanManageAsync(reservation.VenueId, userId))
                return (reservation, false, null);

            return (null, false, new ErrorResult("Bu yazışma sizə aid deyil."));
        }
    }
}
