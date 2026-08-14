using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;
using Entities.Dtos.Admin;
using Entities.Dtos.Common;
using Microsoft.EntityFrameworkCore;

namespace Business.Concrete
{
    // Mübahisə olanda adminin əlində tərəflərin yazışması olmalıdır — OfferMessage
    // elə bunun üçün platformada saxlanılır. Bu servis həmin yazışmaları
    // tapmaq və oxumaq üçündür.
    //
    // Qəsdən yalnız oxunur: admin nə yaza, nə silə, nə də oxunma vəziyyətini
    // dəyişə bilir. Adminin baxması müştəri ilə saray arasındakı «oxundu»
    // işarəsinə toxunmamalıdır — əks halda saray oxumadığı mesajı oxumuş görünər.
    public class AdminChatManager : IAdminChatService
    {
        private const int MaxPageSize = 50;
        private const int PreviewLength = 120;

        private readonly SadliqDbContext _context;

        public AdminChatManager(SadliqDbContext context)
        {
            _context = context;
        }

        public async Task<IDataResult<PagedResult<AdminChatSummaryDto>>> SearchAsync(AdminChatQueryDto query)
        {
            var page = query.Page < 1 ? 1 : query.Page;
            var size = Math.Clamp(query.PageSize, 1, MaxPageSize);

            // Yazışma yalnız müştəri əlaqəni təsdiqləyəndən sonra mövcuddur
            var offers = _context.VenueOffers
                .Where(o => !o.IsDeleted && o.ChatOpenedAt != null);

            if (query.VenueId is { } venueId)
                offers = offers.Where(o => o.VenueId == venueId);

            if (query.CustomerId is { } customerId)
                offers = offers.Where(o => o.Request!.CustomerId == customerId);

            var search = query.Search?.Trim();
            if (!string.IsNullOrEmpty(search))
            {
                // Saray adı, müştərinin adı və ya mesajın mətni üzrə
                offers = offers.Where(o =>
                    o.Venue!.Name.Contains(search)
                    || (o.Request!.Customer!.FirstName + " " + o.Request.Customer.LastName).Contains(search)
                    || o.Messages.Any(m => !m.IsDeleted && m.Body.Contains(search)));
            }

            // Birbaşa rezervin yazışması: təklif yaranmadığı üçün yuxarıdakı
            // sorğuya düşmür, amma adminin əlində o da olmalıdır.
            var reservations = _context.Reservations
                .Where(r => !r.IsDeleted && r.Messages.Any(m => !m.IsDeleted));

            if (query.VenueId is { } resVenueId)
                reservations = reservations.Where(r => r.VenueId == resVenueId);

            if (query.CustomerId is { } resCustomerId)
                reservations = reservations.Where(r => r.CustomerId == resCustomerId);

            if (!string.IsNullOrEmpty(search))
            {
                reservations = reservations.Where(r =>
                    r.Venue!.Name.Contains(search)
                    || (r.Customer!.FirstName + " " + r.Customer.LastName).Contains(search)
                    || r.Messages.Any(m => !m.IsDeleted && m.Body.Contains(search)));
            }

            var total = await offers.CountAsync() + await reservations.CountAsync();

            // İki mənbə birləşdirilir. Səhifələmə yaddaşda aparılır, ona görə hər
            // mənbədən yalnız bu səhifəyə çata biləcək qədər sətir çəkilir —
            // əks halda siyahı böyüdükcə bütün yazışmalar yaddaşa yığılardı.
            var ceiling = page * size;

            var offerRows = await offers
                .OrderByDescending(o => o.Messages
                    .Where(m => !m.IsDeleted)
                    .Max(m => (DateTime?)m.CreatedAt) ?? o.ChatOpenedAt)
                .Take(ceiling)
                .Select(o => new
                {
                    Offer = o,
                    Customer = o.Request!.Customer,
                    VenueName = o.Venue!.Name,
                    RequestGuestCount = o.Request.GuestCount,
                    Messages = o.Messages.Where(m => !m.IsDeleted).ToList()
                })
                .ToListAsync();

            var reservationRows = await reservations
                .OrderByDescending(r => r.Messages
                    .Where(m => !m.IsDeleted)
                    .Max(m => (DateTime?)m.CreatedAt))
                .Take(ceiling)
                .Select(r => new
                {
                    Reservation = r,
                    Customer = r.Customer,
                    VenueName = r.Venue!.Name,
                    Messages = r.Messages.Where(m => !m.IsDeleted).ToList()
                })
                .ToListAsync();

            var merged = new List<AdminChatSummaryDto>();

            foreach (var r in offerRows)
            {
                var last = r.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();

                merged.Add(new AdminChatSummaryDto
                {
                    ThreadKind = "Offer",
                    OfferId = r.Offer.Id,
                    RequestId = r.Offer.RequestId,
                    VenueId = r.Offer.VenueId,
                    VenueName = r.VenueName,
                    CustomerId = r.Customer?.Id ?? 0,
                    CustomerName = FullName(r.Customer),
                    EventDate = r.Offer.EventDate.ToDateTime(TimeOnly.MinValue),
                    GuestCount = r.RequestGuestCount,
                    ChatOpenedAt = r.Offer.ChatOpenedAt!.Value,
                    MessageCount = r.Messages.Count,
                    LastMessageAt = last?.CreatedAt,
                    LastMessagePreview = last is null ? string.Empty : Preview(last.Body),
                    LastMessageFromCustomer = last?.IsFromCustomer ?? false
                });
            }

            foreach (var r in reservationRows)
            {
                var last = r.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();

                merged.Add(new AdminChatSummaryDto
                {
                    ThreadKind = "Reservation",
                    ReservationId = r.Reservation.Id,
                    VenueId = r.Reservation.VenueId,
                    VenueName = r.VenueName,
                    CustomerId = r.Customer?.Id ?? 0,
                    CustomerName = FullName(r.Customer),
                    EventDate = r.Reservation.EventDate.ToDateTime(TimeOnly.MinValue),
                    GuestCount = r.Reservation.GuestCount,

                    // Rezerv yazışmasının ayrıca «açılma» anı yoxdur — söhbət ilk
                    // mesajla başlayır, ona görə siyahıda o tarix göstərilir.
                    ChatOpenedAt = r.Messages.Min(m => m.CreatedAt),
                    MessageCount = r.Messages.Count,
                    LastMessageAt = last?.CreatedAt,
                    LastMessagePreview = last is null ? string.Empty : Preview(last.Body),
                    LastMessageFromCustomer = last?.IsFromCustomer ?? false
                });
            }

            var items = merged
                .OrderByDescending(m => m.LastMessageAt ?? m.ChatOpenedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .ToList();

            return new SuccessDataResult<PagedResult<AdminChatSummaryDto>>(new PagedResult<AdminChatSummaryDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = size
            });
        }

        public async Task<IDataResult<AdminChatDetailDto>> GetAsync(int offerId)
        {
            var offer = await _context.VenueOffers
                .Where(o => !o.IsDeleted && o.Id == offerId)
                .Select(o => new
                {
                    o.Id,
                    o.RequestId,
                    o.EventDate,
                    o.PricePerPerson,
                    o.Status,
                    o.ChatOpenedAt,
                    VenueName = o.Venue!.Name,
                    OwnerId = o.Venue.OwnerId,
                    Customer = o.Request!.Customer,
                    GuestCount = o.Request.GuestCount
                })
                .FirstOrDefaultAsync();

            if (offer is null)
                return new ErrorDataResult<AdminChatDetailDto>("Təklif tapılmadı.");

            if (offer.ChatOpenedAt is null)
                return new ErrorDataResult<AdminChatDetailDto>("Bu təklif üzrə yazışma açılmayıb.");

            var owner = await _context.Users.FirstOrDefaultAsync(u => u.Id == offer.OwnerId);

            // Oxunma vəziyyətinə toxunulmur — bu, tərəflər arasındakı işarədir
            var messages = await _context.OfferMessages
                .Where(m => m.OfferId == offerId && !m.IsDeleted)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new AdminChatMessageDto
                {
                    Id = m.Id,
                    IsFromCustomer = m.IsFromCustomer,
                    SenderName = m.Sender == null
                        ? "—"
                        : (m.Sender.FirstName + " " + m.Sender.LastName).Trim(),
                    Body = m.Body,
                    CreatedAt = m.CreatedAt,
                    IsRead = m.ReadAt != null,
                    IsHidden = m.IsHidden,
                    HiddenReason = m.HiddenReason,
                    HiddenAt = m.HiddenAt
                })
                .ToListAsync();

            return new SuccessDataResult<AdminChatDetailDto>(new AdminChatDetailDto
            {
                ThreadKind = "Offer",
                OfferId = offer.Id,
                RequestId = offer.RequestId,
                VenueName = offer.VenueName,
                OwnerName = FullName(owner),
                CustomerName = FullName(offer.Customer),
                EventDate = offer.EventDate.ToDateTime(TimeOnly.MinValue),
                GuestCount = offer.GuestCount,
                PricePerPerson = offer.PricePerPerson,
                OfferStatus = StatusLabel(offer.Status),
                ChatOpenedAt = offer.ChatOpenedAt.Value,
                Messages = messages
            });
        }

        // Birbaşa rezervin yazışması. Təklif lenti ilə eyni görünüşdə açılır,
        // fərq yalnız kökündədir: burada təklif yoxdur, rezervin özü var.
        public async Task<IDataResult<AdminChatDetailDto>> GetReservationAsync(int reservationId)
        {
            var reservation = await _context.Reservations
                .Where(r => !r.IsDeleted && r.Id == reservationId)
                .Select(r => new
                {
                    r.Id,
                    r.EventDate,
                    r.GuestCount,
                    r.Status,
                    VenueName = r.Venue!.Name,
                    OwnerId = r.Venue.OwnerId,
                    Customer = r.Customer,
                    PricePerPerson = r.Menu!.PricePerPerson
                })
                .FirstOrDefaultAsync();

            if (reservation is null)
                return new ErrorDataResult<AdminChatDetailDto>("Rezerv tapılmadı.");

            var owner = await _context.Users.FirstOrDefaultAsync(u => u.Id == reservation.OwnerId);

            // Oxunma vəziyyətinə toxunulmur — bu, tərəflər arasındakı işarədir
            var messages = await _context.OfferMessages
                .Where(m => m.ReservationId == reservationId && !m.IsDeleted)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new AdminChatMessageDto
                {
                    Id = m.Id,
                    IsFromCustomer = m.IsFromCustomer,
                    SenderName = m.Sender == null
                        ? "—"
                        : (m.Sender.FirstName + " " + m.Sender.LastName).Trim(),
                    Body = m.Body,
                    CreatedAt = m.CreatedAt,
                    IsRead = m.ReadAt != null,
                    IsHidden = m.IsHidden,
                    HiddenReason = m.HiddenReason,
                    HiddenAt = m.HiddenAt
                })
                .ToListAsync();

            if (messages.Count == 0)
                return new ErrorDataResult<AdminChatDetailDto>("Bu rezerv üzrə yazışma yoxdur.");

            return new SuccessDataResult<AdminChatDetailDto>(new AdminChatDetailDto
            {
                ThreadKind = "Reservation",
                ReservationId = reservation.Id,
                VenueName = reservation.VenueName,
                OwnerName = FullName(owner),
                CustomerName = FullName(reservation.Customer),
                EventDate = reservation.EventDate.ToDateTime(TimeOnly.MinValue),
                GuestCount = reservation.GuestCount,
                PricePerPerson = reservation.PricePerPerson,
                OfferStatus = ReservationStatusLabel(reservation.Status),
                ChatOpenedAt = messages[0].CreatedAt,
                Messages = messages
            });
        }

        // Qanunsuz və ya təhqiredici mesajı tərəflərdən gizlədir. Mesaj bazadan
        // silinmir — admin onu səbəbi ilə birlikdə görməkdə davam edir.
        public async Task<IResult> SetHiddenAsync(int messageId, SetMessageHiddenDto dto, int adminUserId)
        {
            var message = await _context.OfferMessages
                .FirstOrDefaultAsync(m => m.Id == messageId && !m.IsDeleted);

            if (message is null)
                return new ErrorResult("Mesaj tapılmadı.");

            if (message.IsHidden == dto.IsHidden)
                return new ErrorResult(dto.IsHidden ? "Mesaj artıq gizlədilib." : "Mesaj artıq görünür.");

            if (dto.IsHidden && string.IsNullOrWhiteSpace(dto.Reason))
                return new ErrorResult("Gizlətmə səbəbini yazın.");

            message.IsHidden = dto.IsHidden;
            message.HiddenReason = dto.IsHidden ? dto.Reason!.Trim() : null;
            message.HiddenAt = dto.IsHidden ? DateTime.UtcNow : null;
            message.HiddenByUserId = dto.IsHidden ? adminUserId : null;

            await _context.SaveChangesAsync();

            return new SuccessResult(dto.IsHidden
                ? "Mesaj tərəflərdən gizlədildi."
                : "Mesaj yenidən göstərilir.");
        }

        private static string FullName(User? user) =>
            user is null ? "—" : $"{user.FirstName} {user.LastName}".Trim();

        private static string Preview(string body) =>
            body.Length > PreviewLength ? body[..PreviewLength] + "…" : body;

        private static string StatusLabel(VenueOfferStatus status) => status switch
        {
            VenueOfferStatus.Sent => "Göndərilib",
            VenueOfferStatus.Accepted => "Qəbul edilib",
            VenueOfferStatus.Declined => "Uyğun gəlmədi",
            _ => "Geri götürülüb"
        };

        private static string ReservationStatusLabel(ReservationStatus status) => status switch
        {
            ReservationStatus.Pending => "Cavab gözləyir",
            ReservationStatus.Confirmed => "Təsdiqlənib",
            ReservationStatus.Rejected => "Rədd edilib",
            ReservationStatus.Cancelled => "Ləğv edilib",
            ReservationStatus.CounterOffered => "Əks-təklif göndərilib",
            _ => "Vaxtı bitib"
        };
    }
}
