using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;
using Entities.Dtos.VenueRequest;
using Microsoft.EntityFrameworkCore;

namespace Business.Concrete
{
    // Sorğu → təkliflər → qəbul. Kataloqdan fərqi budur: müştəri axtarmır,
    // saraylar onu tapır. Sahib üçün isə bu, boş qalan tarixi satmaq yoludur.
    public class VenueRequestManager : IVenueRequestService
    {
        private const int RequestLifetimeDays = 7;
        private const int MaxOpenRequestsPerCustomer = 1;
        private const int MaxOffersPerRequest = 12;

        private readonly IVenueRequestDal _requestDal;
        private readonly IVenueOfferDal _offerDal;
        private readonly IVenueDal _venueDal;
        private readonly IHallDal _hallDal;
        private readonly IMenuDal _menuDal;
        private readonly IReservationDal _reservationDal;
        private readonly IHallAvailabilityDal _availabilityDal;
        private readonly IVenueAccessService _access;
        private readonly INotificationService _notifications;
        private readonly IUserDal _userDal;
        private readonly SadliqDbContext _context;
        private readonly IAuditContext _audit;

        public VenueRequestManager(
            IVenueRequestDal requestDal,
            IVenueOfferDal offerDal,
            IVenueDal venueDal,
            IHallDal hallDal,
            IMenuDal menuDal,
            IReservationDal reservationDal,
            IHallAvailabilityDal availabilityDal,
            IVenueAccessService access,
            INotificationService notifications,
            IUserDal userDal,
            SadliqDbContext context,
            IAuditContext audit)
        {
            _requestDal = requestDal;
            _offerDal = offerDal;
            _venueDal = venueDal;
            _hallDal = hallDal;
            _menuDal = menuDal;
            _reservationDal = reservationDal;
            _availabilityDal = availabilityDal;
            _access = access;
            _notifications = notifications;
            _userDal = userDal;
            _context = context;
            _audit = audit;
        }

        // ————— müştəri —————

        public async Task<IDataResult<VenueRequestDto>> CreateAsync(CreateVenueRequestDto dto, int customerId)
        {
            if (string.IsNullOrWhiteSpace(dto.City))
                return new ErrorDataResult<VenueRequestDto>("Şəhəri seçin.");

            if (dto.GuestCount < 10)
                return new ErrorDataResult<VenueRequestDto>("Qonaq sayı ən azı 10 olmalıdır.");

            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

            if (!dto.IsDateFlexible)
            {
                if (dto.EventDate is null)
                    return new ErrorDataResult<VenueRequestDto>(
                        "Tarix seçin və ya «tarixim çevikdir» variantını işarələyin.");

                if (dto.EventDate <= today)
                    return new ErrorDataResult<VenueRequestDto>("Tarix bugündən sonrakı gün olmalıdır.");
            }

            // Bir müştəri eyni anda bir sorğu — bazar spam olmasın
            var openQuery = await _requestDal.GetListAsync(
                r => r.CustomerId == customerId && r.Status == VenueRequestStatus.Open);

            if (await openQuery.CountAsync() >= MaxOpenRequestsPerCustomer)
                return new ErrorDataResult<VenueRequestDto>(
                    "Açıq sorğunuz var. Yenisini göndərmək üçün əvvəlkini bağlayın.");

            var request = new VenueRequest
            {
                CustomerId = customerId,
                City = dto.City.Trim(),
                EventDate = dto.IsDateFlexible ? null : dto.EventDate,
                IsDateFlexible = dto.IsDateFlexible,
                GuestCount = dto.GuestCount,
                BudgetPerPerson = dto.BudgetPerPerson,
                NeedsParking = dto.NeedsParking,
                Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim(),
                Status = VenueRequestStatus.Open,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(RequestLifetimeDays)
            };

            await _requestDal.AddAsync(request);

            await NotifyMatchingOwnersAsync(request);

            _audit.Set(
                "VenueRequestCreated",
                $"{request.City} üzrə {request.GuestCount} nəfərlik sorğu paylaşdı",
                "VenueRequest", request.Id, null);

            return new SuccessDataResult<VenueRequestDto>(
                await ToDtoAsync(request),
                "Sorğunuz uyğun saraylara göndərildi. Təkliflər gəldikcə bildiriş alacaqsınız.");
        }

        public async Task<IDataResult<List<VenueRequestDto>>> GetMineAsync(int customerId)
        {
            var query = await _requestDal.GetListAsync(r => r.CustomerId == customerId);

            var requests = await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var list = new List<VenueRequestDto>();
            foreach (var request in requests)
                list.Add(await ToDtoAsync(request));

            return new SuccessDataResult<List<VenueRequestDto>>(list);
        }

        public async Task<IResult> CloseAsync(int requestId, int customerId)
        {
            var request = await _requestDal.GetAsync(r => r.Id == requestId && r.CustomerId == customerId);
            if (request is null) return new ErrorResult("Sorğu tapılmadı.");

            if (request.Status != VenueRequestStatus.Open)
                return new ErrorResult("Bu sorğu artıq bağlıdır.");

            request.Status = VenueRequestStatus.Closed;
            request.ClosedAt = DateTime.UtcNow;
            await _requestDal.UpdateAsync(request);

            await CloseOtherOffersAsync(request.Id, null);

            return new SuccessResult("Sorğu bağlandı.");
        }

        // ————— saray —————

        // Sahibin saraylarına uyğun gələn açıq sorğular
        public async Task<IDataResult<List<OpenRequestDto>>> GetOpenForOwnerAsync(int ownerId)
        {
            var venueIds = await _access.GetManagedVenueIdsAsync(ownerId);
            if (venueIds.Count == 0)
                return new SuccessDataResult<List<OpenRequestDto>>(new List<OpenRequestDto>());

            var venues = await _context.Venues
                .Where(v => venueIds.Contains(v.Id) && v.IsApproved)
                .Include(v => v.Halls)
                .Include(v => v.Menus)
                .ToListAsync();

            if (venues.Count == 0)
                return new SuccessDataResult<List<OpenRequestDto>>(new List<OpenRequestDto>());

            var now = DateTime.UtcNow;

            var requests = await _context.VenueRequests
                .Where(r => r.Status == VenueRequestStatus.Open && r.ExpiresAt > now)
                .Include(r => r.Offers)
                .OrderByDescending(r => r.CreatedAt)
                .Take(100)
                .ToListAsync();

            var result = new List<OpenRequestDto>();

            foreach (var request in requests)
            {
                var matching = venues
                    .Where(v => Matches(v, request))
                    .Select(v => new MatchingVenueDto
                    {
                        VenueId = v.Id,
                        VenueName = v.Name,
                        Halls = v.Halls
                            .Where(h => h.Capacity >= request.GuestCount)
                            .Select(h => new HallOptionDto { Id = h.Id, Name = h.Name, Capacity = h.Capacity })
                            .ToList(),
                        Menus = v.Menus
                            .Select(m => new MenuOptionDto
                            {
                                Id = m.Id,
                                Name = m.Name,
                                PricePerPerson = m.PricePerPerson
                            })
                            .ToList()
                    })
                    .Where(m => m.Halls.Count > 0 && m.Menus.Count > 0)
                    .ToList();

                if (matching.Count == 0) continue;

                result.Add(new OpenRequestDto
                {
                    Id = request.Id,
                    City = request.City,
                    EventDate = request.EventDate,
                    IsDateFlexible = request.IsDateFlexible,
                    GuestCount = request.GuestCount,
                    BudgetPerPerson = request.BudgetPerPerson,
                    NeedsParking = request.NeedsParking,
                    Note = request.Note,
                    CreatedAt = request.CreatedAt,
                    ExpiresAt = request.ExpiresAt,
                    OfferCount = request.Offers.Count(o => o.Status == VenueOfferStatus.Sent),
                    MatchingVenues = matching,
                    OfferedVenueIds = request.Offers
                        .Where(o => o.Status != VenueOfferStatus.Withdrawn)
                        .Select(o => o.VenueId)
                        .ToList()
                });
            }

            return new SuccessDataResult<List<OpenRequestDto>>(result);
        }

        // Şəhər, tutum və parkinq üzrə ilkin uyğunluq
        private static bool Matches(Venue venue, VenueRequest request)
        {
            if (!string.Equals(venue.City?.Trim(), request.City?.Trim(), StringComparison.OrdinalIgnoreCase))
                return false;

            if (request.NeedsParking && (venue.ParkingCapacity ?? 0) <= 0)
                return false;

            return venue.Halls.Any(h => !h.IsDeleted && h.Capacity >= request.GuestCount);
        }

        public async Task<IResult> CreateOfferAsync(CreateVenueOfferDto dto, int ownerId)
        {
            var request = await _requestDal.GetAsync(r => r.Id == dto.RequestId);
            if (request is null) return new ErrorResult("Sorğu tapılmadı.");

            if (request.Status != VenueRequestStatus.Open)
                return new ErrorResult("Bu sorğu bağlanıb.");

            if (request.ExpiresAt <= DateTime.UtcNow)
                return new ErrorResult("Bu sorğunun vaxtı keçib.");

            if (!await _access.CanModifyAsync(dto.VenueId, ownerId))
                return new ErrorResult("Bu saray sizə aid deyil.");

            var venue = await _venueDal.GetAsync(v => v.Id == dto.VenueId);
            if (venue is null || !venue.IsApproved)
                return new ErrorResult("Saray tapılmadı və ya təsdiqlənməyib.");

            var hall = await _hallDal.GetAsync(h => h.Id == dto.HallId);
            if (hall is null || hall.VenueId != dto.VenueId)
                return new ErrorResult("Seçilən zal bu saraya aid deyil.");

            if (hall.Capacity < request.GuestCount)
                return new ErrorResult(
                    $"«{hall.Name}» zalının tutumu {hall.Capacity} nəfərdir, sorğu isə {request.GuestCount} nəfərlikdir.");

            var menu = await _menuDal.GetAsync(m => m.Id == dto.MenuId);
            if (menu is null || menu.VenueId != dto.VenueId)
                return new ErrorResult("Seçilən menyu bu saraya aid deyil.");

            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            if (dto.EventDate <= today)
                return new ErrorResult("Təklif olunan tarix bugündən sonrakı gün olmalıdır.");

            // Müştəri konkret tarix yazıbsa, saray onu dəyişə bilməz
            if (!request.IsDateFlexible && request.EventDate.HasValue && dto.EventDate != request.EventDate)
                return new ErrorResult("Müştəri konkret tarix göstərib — təklif həmin tarixə olmalıdır.");

            if (dto.PricePerPerson <= 0)
                return new ErrorResult("Nəfər başına qiyməti yazın.");

            if (string.IsNullOrWhiteSpace(dto.Message) || dto.Message.Trim().Length < 10)
                return new ErrorResult("Müştəriyə qısa təqdimat yazın — niyə məhz sizin saray.");

            // Həmin gün zal boş olmalıdır
            var slot = await _availabilityDal.GetAsync(
                a => a.HallId == dto.HallId && a.Date == dto.EventDate);

            if (slot is not null && slot.Status != AvailabilityStatus.Available)
                return new ErrorResult("Seçdiyiniz gün bu zalda boş deyil.");

            // Bir saray bir sorğuya bir təklif göndərir; köhnəsi yenilənir
            var existing = await _offerDal.GetAsync(
                o => o.RequestId == request.Id && o.VenueId == dto.VenueId
                     && o.Status == VenueOfferStatus.Sent);

            if (existing is not null)
            {
                existing.HallId = dto.HallId;
                existing.MenuId = dto.MenuId;
                existing.EventDate = dto.EventDate;
                existing.PricePerPerson = dto.PricePerPerson;
                existing.Message = dto.Message.Trim();
                existing.CreatedAt = DateTime.UtcNow;
                await _offerDal.UpdateAsync(existing);

                return new SuccessResult("Təklifiniz yeniləndi.");
            }

            var offerCountQuery = await _offerDal.GetListAsync(
                o => o.RequestId == request.Id && o.Status == VenueOfferStatus.Sent);

            if (await offerCountQuery.CountAsync() >= MaxOffersPerRequest)
                return new ErrorResult("Bu sorğu kifayət qədər təklif alıb.");

            var offer = new VenueOffer
            {
                RequestId = request.Id,
                VenueId = dto.VenueId,
                HallId = dto.HallId,
                MenuId = dto.MenuId,
                EventDate = dto.EventDate,
                PricePerPerson = dto.PricePerPerson,
                Message = dto.Message.Trim(),
                CreatedByUserId = ownerId,
                Status = VenueOfferStatus.Sent,
                CreatedAt = DateTime.UtcNow
            };

            await _offerDal.AddAsync(offer);

            await _notifications.NotifyAsync(
                request.CustomerId,
                new NotificationMessage(
                    NotificationType.ReservationStatusChanged,
                    $"«{venue.Name}» sizə təklif göndərdi",
                    $"{dto.EventDate:dd.MM.yyyy} · {hall.Name} · {menu.Name}\n" +
                    $"{dto.PricePerPerson} ₼ / nəfər · cəmi {dto.PricePerPerson * request.GuestCount} ₼\n\n" +
                    offer.Message,
                    "/sorgularim"));

            _audit.Set(
                "VenueOfferSent",
                $"«{venue.Name}» adından sorğuya təklif göndərdi",
                "VenueRequest", request.Id, venue.Id);

            return new SuccessResult("Təklifiniz müştəriyə göndərildi.");
        }

        public async Task<IResult> WithdrawOfferAsync(int offerId, int ownerId)
        {
            var offer = await _offerDal.GetAsync(o => o.Id == offerId);
            if (offer is null) return new ErrorResult("Təklif tapılmadı.");

            if (!await _access.CanModifyAsync(offer.VenueId, ownerId))
                return new ErrorResult("Bu təklif sizə aid deyil.");

            if (offer.Status != VenueOfferStatus.Sent)
                return new ErrorResult("Bu təklifə artıq cavab verilib.");

            offer.Status = VenueOfferStatus.Withdrawn;
            offer.RespondedAt = DateTime.UtcNow;
            await _offerDal.UpdateAsync(offer);

            return new SuccessResult("Təklif geri götürüldü.");
        }

        // ————— təklifin qəbulu —————

        // Qəbul rezervə çevrilir: gün tutulur, sorğu bağlanır, qalan saraylara xəbər gedir
        public async Task<IResult> AcceptOfferAsync(int offerId, int customerId)
        {
            var offer = await _offerDal.GetAsync(o => o.Id == offerId);
            if (offer is null) return new ErrorResult("Təklif tapılmadı.");

            if (offer.Status != VenueOfferStatus.Sent)
                return new ErrorResult("Bu təklifə artıq cavab verilib.");

            var request = await _requestDal.GetAsync(r => r.Id == offer.RequestId);
            if (request is null || request.CustomerId != customerId)
                return new ErrorResult("Bu təklif sizə aid deyil.");

            if (request.Status != VenueRequestStatus.Open)
                return new ErrorResult("Bu sorğu artıq bağlıdır.");

            // Gün hələ də boşdurmu
            var slot = await _availabilityDal.GetAsync(
                a => a.HallId == offer.HallId && a.Date == offer.EventDate);

            if (slot is not null && slot.Status != AvailabilityStatus.Available)
                return new ErrorResult("Təəssüf, bu gün artıq tutulub. Başqa təklif seçin.");

            var customer = await _userDal.GetAsync(u => u.Id == customerId);

            var reservation = new Reservation
            {
                CustomerId = customerId,
                VenueId = offer.VenueId,
                HallId = offer.HallId,
                MenuId = offer.MenuId,
                EventDate = offer.EventDate,
                GuestCount = request.GuestCount,
                ContactPhone = customer?.Phone ?? string.Empty,
                Note = request.Note,
                Status = ReservationStatus.Confirmed,
                CreatedAt = DateTime.UtcNow
            };

            await _reservationDal.AddAsync(reservation);

            if (slot is null)
            {
                await _availabilityDal.AddAsync(new HallAvailability
                {
                    HallId = offer.HallId,
                    Date = offer.EventDate,
                    Status = AvailabilityStatus.Booked
                });
            }
            else
            {
                slot.Status = AvailabilityStatus.Booked;
                slot.HoldExpiresAt = null;
                await _availabilityDal.UpdateAsync(slot);
            }

            offer.Status = VenueOfferStatus.Accepted;
            offer.RespondedAt = DateTime.UtcNow;
            await _offerDal.UpdateAsync(offer);

            request.Status = VenueRequestStatus.Fulfilled;
            request.ClosedAt = DateTime.UtcNow;
            request.ReservationId = reservation.Id;
            await _requestDal.UpdateAsync(request);

            await CloseOtherOffersAsync(request.Id, offer.Id);

            var venue = await _venueDal.GetAsync(v => v.Id == offer.VenueId);

            if (venue is not null)
                await _notifications.NotifyAsync(
                    venue.OwnerId,
                    new NotificationMessage(
                        NotificationType.ReservationStatusChanged,
                        "Təklifiniz qəbul edildi",
                        $"{customer?.FullName ?? "Müştəri"} təklifinizi seçdi. " +
                        $"Rezerv təsdiqləndi: {offer.EventDate:dd.MM.yyyy}, {request.GuestCount} qonaq.",
                        "/panel"));

            _audit.Set(
                "VenueOfferAccepted",
                $"«{venue?.Name}» sarayının təklifini qəbul etdi",
                "VenueRequest", request.Id, offer.VenueId);

            return new SuccessResult("Təklif qəbul edildi, rezerviniz yaradıldı.");
        }

        public async Task<IResult> DeclineOfferAsync(int offerId, int customerId)
        {
            var offer = await _offerDal.GetAsync(o => o.Id == offerId);
            if (offer is null) return new ErrorResult("Təklif tapılmadı.");

            var request = await _requestDal.GetAsync(r => r.Id == offer.RequestId);
            if (request is null || request.CustomerId != customerId)
                return new ErrorResult("Bu təklif sizə aid deyil.");

            if (offer.Status != VenueOfferStatus.Sent)
                return new ErrorResult("Bu təklifə artıq cavab verilib.");

            offer.Status = VenueOfferStatus.Declined;
            offer.RespondedAt = DateTime.UtcNow;
            await _offerDal.UpdateAsync(offer);

            return new SuccessResult("Təklif siyahıdan çıxarıldı.");
        }

        // Qəbul olunandan sonra qalan təkliflər bağlanır və saraylara xəbər gedir
        private async Task CloseOtherOffersAsync(int requestId, int? acceptedOfferId)
        {
            var query = await _offerDal.GetListAsync(
                o => o.RequestId == requestId && o.Status == VenueOfferStatus.Sent);

            var others = await query.ToListAsync();

            foreach (var other in others)
            {
                if (acceptedOfferId.HasValue && other.Id == acceptedOfferId.Value) continue;

                var tracked = await _offerDal.GetAsync(o => o.Id == other.Id);
                if (tracked is null) continue;

                tracked.Status = VenueOfferStatus.Declined;
                tracked.RespondedAt = DateTime.UtcNow;
                await _offerDal.UpdateAsync(tracked);

                var venue = await _venueDal.GetAsync(v => v.Id == tracked.VenueId);
                if (venue is null) continue;

                await _notifications.NotifyAsync(
                    venue.OwnerId,
                    new NotificationMessage(
                        NotificationType.SystemMessage,
                        "Sorğu bağlandı",
                        $"«{venue.Name}» adından göndərdiyiniz təklif üzrə müştəri başqa seçim etdi.",
                        "/panel"));
            }
        }

        // ————— arxa fon —————

        public async Task<int> ExpireOldAsync(CancellationToken token = default)
        {
            var now = DateTime.UtcNow;

            var expired = await _context.VenueRequests
                .Where(r => r.Status == VenueRequestStatus.Open && r.ExpiresAt <= now)
                .ToListAsync(token);

            foreach (var request in expired)
            {
                request.Status = VenueRequestStatus.Expired;
                request.ClosedAt = now;
            }

            foreach (var request in expired)
            {
                var offers = await _context.VenueOffers
                    .CountAsync(o => o.RequestId == request.Id && o.Status == VenueOfferStatus.Sent, token);

                await _notifications.NotifyAsync(
                    request.CustomerId,
                    new NotificationMessage(
                        NotificationType.SystemMessage,
                        "Sorğunuzun müddəti bitdi",
                        offers > 0
                            ? $"{request.City} üzrə sorğunuza {offers} təklif gəlmişdi, amma seçim etmədiniz. " +
                              "İstəsəniz yeni sorğu paylaşa bilərsiniz."
                            : "Sorğunuza təklif gəlmədi. Şərtləri bir az yumşaldıb (tarix, büdcə) " +
                              "yenidən cəhd edə bilərsiniz.",
                        "/sorgularim"));
            }

            if (expired.Count > 0)
                await _context.SaveChangesAsync(token);

            // Bitməsinə bir gün qalmışlar: müştəri təklifləri gözdən qaçırmasın
            var soon = now.AddHours(24);

            var ending = await _context.VenueRequests
                .Where(r => r.Status == VenueRequestStatus.Open
                            && r.ExpiryWarningSentAt == null
                            && r.ExpiresAt > now
                            && r.ExpiresAt <= soon)
                .ToListAsync(token);

            if (ending.Count > 0)
            {
                foreach (var request in ending)
                    request.ExpiryWarningSentAt = now;

                await _context.SaveChangesAsync(token);

                foreach (var request in ending)
                {
                    var offers = await _context.VenueOffers
                        .CountAsync(o => o.RequestId == request.Id && o.Status == VenueOfferStatus.Sent, token);

                    await _notifications.NotifyAsync(
                        request.CustomerId,
                        new NotificationMessage(
                            NotificationType.ReservationReminder,
                            "Sorğunuza bir gün qalıb",
                            offers > 0
                                ? $"Sizə {offers} təklif gəlib. Sorğu sabah bağlanacaq — " +
                                  "təklifləri müqayisə edib seçim edin."
                                : "Sorğunuz sabah bağlanacaq və hələ təklif gəlməyib. " +
                                  "Şərtləri dəyişib yeni sorğu paylaşa bilərsiniz.",
                            "/sorgularim"));
                }
            }

            return expired.Count;
        }

        // ————— köməkçilər —————

        // Sorğu paylaşılanda uyğun sarayların sahiblərinə xəbər verilir
        private async Task NotifyMatchingOwnersAsync(VenueRequest request)
        {
            var venues = await _context.Venues
                .Where(v => v.IsApproved)
                .Include(v => v.Halls)
                .ToListAsync();

            var ownerIds = venues
                .Where(v => Matches(v, request))
                .Select(v => v.OwnerId)
                .Distinct()
                .ToList();

            if (ownerIds.Count == 0) return;

            var date = request.IsDateFlexible
                ? "tarix çevikdir"
                : request.EventDate?.ToString("dd.MM.yyyy") ?? "tarix göstərilməyib";

            var budget = request.BudgetPerPerson.HasValue
                ? $"{request.BudgetPerPerson} ₼ / nəfər büdcə"
                : "büdcə göstərilməyib";

            await _notifications.NotifyManyAsync(
                ownerIds,
                new NotificationMessage(
                    NotificationType.SystemMessage,
                    "Yeni sorğu — təklif göndərə bilərsiniz",
                    $"{request.City} · {request.GuestCount} qonaq · {date} · {budget}\n\n" +
                    "Sorğu 7 gün açıq qalır. Panelin «Açıq sorğular» bölməsindən təklif göndərin.",
                    "/panel"),
                NotificationChannels.InApp);
        }

        private async Task<VenueRequestDto> ToDtoAsync(VenueRequest request)
        {
            var offers = await _context.VenueOffers
                .Where(o => o.RequestId == request.Id && o.Status != VenueOfferStatus.Withdrawn)
                .Include(o => o.Venue)
                .Include(o => o.Hall)
                .Include(o => o.Menu)
                .Include(o => o.Messages)
                .OrderBy(o => o.PricePerPerson)
                .ToListAsync();

            var venueIds = offers.Select(o => o.VenueId).Distinct().ToList();

            // Reytinq müqayisə üçün lazımdır — təklifi ucuz olan həmişə yaxşı deyil
            var ratings = await _context.Reviews
                .Where(r => venueIds.Contains(r.VenueId))
                .GroupBy(r => r.VenueId)
                .Select(g => new { VenueId = g.Key, Average = g.Average(r => (double)r.Rating), Count = g.Count() })
                .ToDictionaryAsync(x => x.VenueId);

            return new VenueRequestDto
            {
                Id = request.Id,
                City = request.City,
                EventDate = request.EventDate,
                IsDateFlexible = request.IsDateFlexible,
                GuestCount = request.GuestCount,
                BudgetPerPerson = request.BudgetPerPerson,
                NeedsParking = request.NeedsParking,
                Note = request.Note,
                Status = request.Status,
                StatusLabel = StatusLabel(request.Status),
                CreatedAt = request.CreatedAt,
                ExpiresAt = request.ExpiresAt,
                ReservationId = request.ReservationId,
                Offers = offers.Select(o => new VenueOfferDto
                {
                    Id = o.Id,
                    VenueId = o.VenueId,
                    VenueName = o.Venue?.Name ?? "—",
                    VenueImageUrl = o.Venue?.CoverImageUrl,
                    City = o.Venue?.City ?? string.Empty,
                    Address = o.Venue?.Address ?? string.Empty,
                    HallName = o.Hall?.Name ?? "—",
                    HallCapacity = o.Hall?.Capacity ?? 0,
                    MenuName = o.Menu?.Name ?? "—",
                    EventDate = o.EventDate,
                    PricePerPerson = o.PricePerPerson,
                    TotalPrice = o.PricePerPerson * request.GuestCount,
                    Message = o.Message,
                    Rating = ratings.TryGetValue(o.VenueId, out var r) ? Math.Round(r.Average, 1) : null,
                    ReviewCount = ratings.TryGetValue(o.VenueId, out var rc) ? rc.Count : 0,
                    ParkingCapacity = o.Venue?.ParkingCapacity,
                    Status = o.Status,
                    CreatedAt = o.CreatedAt,
                    IsChatOpen = o.ChatOpenedAt is not null,
                    UnreadCount = o.Messages.Count(m => !m.IsFromCustomer && m.ReadAt is null),
                    LastMessage = o.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault()?.Body,
                    LastMessageAt = o.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault()?.CreatedAt
                }).ToList()
            };
        }

        private static string StatusLabel(VenueRequestStatus status) => status switch
        {
            VenueRequestStatus.Open => "Açıqdır",
            VenueRequestStatus.Fulfilled => "Rezerv yaradıldı",
            VenueRequestStatus.Closed => "Bağlanıb",
            _ => "Vaxtı keçib"
        };

        // ————— sahibin təklifləri —————

        public async Task<IDataResult<List<MyOfferDto>>> GetMyOffersAsync(int ownerId)
        {
            var venueIds = await _access.GetManagedVenueIdsAsync(ownerId);
            if (venueIds.Count == 0)
                return new SuccessDataResult<List<MyOfferDto>>(new List<MyOfferDto>());

            var offers = await _context.VenueOffers
                .Where(o => venueIds.Contains(o.VenueId))
                .Include(o => o.Venue)
                .Include(o => o.Request)!.ThenInclude(r => r!.Customer)
                .Include(o => o.Messages)
                .OrderByDescending(o => o.CreatedAt)
                .Take(100)
                .ToListAsync();

            var list = offers.Select(o => new MyOfferDto
            {
                Id = o.Id,
                RequestId = o.RequestId,
                VenueId = o.VenueId,
                VenueName = o.Venue?.Name ?? "—",

                // Müştərinin adı yalnız söhbət açılandan sonra göstərilir
                CustomerName = o.ChatOpenedAt is null
                    ? "Müştəri"
                    : $"{o.Request?.Customer?.FirstName} {o.Request?.Customer?.LastName}".Trim(),

                City = o.Request?.City ?? string.Empty,
                GuestCount = o.Request?.GuestCount ?? 0,
                EventDate = o.EventDate,
                PricePerPerson = o.PricePerPerson,
                TotalPrice = o.PricePerPerson * (o.Request?.GuestCount ?? 0),
                Status = o.Status,
                StatusLabel = OfferStatusLabel(o.Status),
                RequestStatus = o.Request?.Status ?? VenueRequestStatus.Closed,
                IsChatOpen = o.ChatOpenedAt is not null,
                UnreadCount = o.Messages.Count(m => m.IsFromCustomer && m.ReadAt is null),
                LastMessage = o.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault()?.Body,
                LastMessageAt = o.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault()?.CreatedAt,
                CreatedAt = o.CreatedAt
            }).ToList();

            return new SuccessDataResult<List<MyOfferDto>>(list);
        }

        // ————— yazışma —————

        // Müştəri təklifi təsdiqləyir: rezerv yaranmır, sadəcə əlaqə açılır.
        // Danışdıqdan sonra müştəri istəsə «bunu seçirəm» deyir.
        public async Task<IResult> ApproveContactAsync(int offerId, int customerId)
        {
            var offer = await _offerDal.GetAsync(o => o.Id == offerId);
            if (offer is null) return new ErrorResult("Təklif tapılmadı.");

            var request = await _requestDal.GetAsync(r => r.Id == offer.RequestId);
            if (request is null || request.CustomerId != customerId)
                return new ErrorResult("Bu təklif sizə aid deyil.");

            if (offer.Status != VenueOfferStatus.Sent)
                return new ErrorResult("Bu təklif artıq qüvvədə deyil.");

            if (offer.ChatOpenedAt is not null)
                return new SuccessResult("Yazışma onsuz da açıqdır.");

            offer.ChatOpenedAt = DateTime.UtcNow;
            await _offerDal.UpdateAsync(offer);

            var venue = await _venueDal.GetAsync(v => v.Id == offer.VenueId);
            var customer = await _userDal.GetAsync(u => u.Id == customerId);

            if (venue is not null)
                await _notifications.NotifyAsync(
                    venue.OwnerId,
                    new NotificationMessage(
                        NotificationType.SystemMessage,
                        "Müştəri təklifinizi təsdiqlədi",
                        $"{customer?.FullName ?? "Müştəri"} «{venue.Name}» üçün göndərdiyiniz təklifi təsdiqlədi " +
                        $"({offer.EventDate:dd.MM.yyyy}, {request.GuestCount} qonaq). " +
                        "Artıq yazışa bilərsiniz — panelin «Təkliflərim» bölməsinə keçin.",
                        "/panel"));

            return new SuccessResult("Yazışma açıldı — sarayla birbaşa danışa bilərsiniz.");
        }

        public async Task<IDataResult<List<OfferMessageDto>>> GetMessagesAsync(int offerId, int userId)
        {
            var (offer, isCustomer, error) = await LoadChatAsync(offerId, userId);
            if (error is not null)
                return new ErrorDataResult<List<OfferMessageDto>>(error.Message);

            // Admin tərəfindən gizlədilmiş mesaj tərəflərə göstərilmir
            var messages = await _context.OfferMessages
                .Where(m => m.OfferId == offerId && !m.IsDeleted && !m.IsHidden)
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

        public async Task<IDataResult<OfferMessageDto>> SendMessageAsync(SendMessageDto dto, int userId)
        {
            if (string.IsNullOrWhiteSpace(dto.Body))
                return new ErrorDataResult<OfferMessageDto>("Mesaj boş ola bilməz.");

            if (dto.Body.Trim().Length > 1000)
                return new ErrorDataResult<OfferMessageDto>("Mesaj 1000 simvoldan uzun ola bilməz.");

            var (offer, isCustomer, error) = await LoadChatAsync(dto.OfferId, userId);
            if (error is not null)
                return new ErrorDataResult<OfferMessageDto>(error.Message);

            var sender = await _userDal.GetAsync(u => u.Id == userId);

            var message = new OfferMessage
            {
                OfferId = dto.OfferId,
                SenderUserId = userId,
                IsFromCustomer = isCustomer,
                Body = dto.Body.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.OfferMessages.Add(message);
            await _context.SaveChangesAsync();

            var request = await _requestDal.GetAsync(r => r.Id == offer!.RequestId);
            var venue = await _venueDal.GetAsync(v => v.Id == offer!.VenueId);

            // Qarşı tərəfə bildiriş: mesajın əvvəli də görünsün ki, açmağa dəyərmi bilsin
            var preview = message.Body.Length > 120 ? message.Body[..120] + "…" : message.Body;

            if (isCustomer && venue is not null)
            {
                await _notifications.NotifyAsync(
                    venue.OwnerId,
                    new NotificationMessage(
                        NotificationType.SystemMessage,
                        $"Yeni mesaj — {sender?.FullName ?? "müştəri"}",
                        $"«{venue.Name}» üzrə yazışma:\n{preview}",
                        "/panel"));
            }
            else if (!isCustomer && request is not null)
            {
                await _notifications.NotifyAsync(
                    request.CustomerId,
                    new NotificationMessage(
                        NotificationType.SystemMessage,
                        $"Yeni mesaj — {venue?.Name ?? "saray"}",
                        preview,
                        "/sorgularim"));
            }

            return new SuccessDataResult<OfferMessageDto>(new OfferMessageDto
            {
                Id = message.Id,
                IsFromCustomer = message.IsFromCustomer,
                SenderName = sender is null ? "—" : $"{sender.FirstName} {sender.LastName}".Trim(),
                Body = message.Body,
                CreatedAt = message.CreatedAt,
                IsRead = false
            });
        }

        // Yazışmaya kimin çıxışı var: sorğunu yazan müştəri və ya sarayın adamı
        private async Task<(VenueOffer? Offer, bool IsCustomer, IResult? Error)> LoadChatAsync(
            int offerId, int userId)
        {
            var offer = await _offerDal.GetAsync(o => o.Id == offerId);
            if (offer is null)
                return (null, false, new ErrorResult("Təklif tapılmadı."));

            if (offer.ChatOpenedAt is null)
                return (null, false, new ErrorResult("Bu təklif üzrə yazışma hələ açılmayıb."));

            var request = await _requestDal.GetAsync(r => r.Id == offer.RequestId);
            if (request is null)
                return (null, false, new ErrorResult("Sorğu tapılmadı."));

            if (request.CustomerId == userId)
                return (offer, true, null);

            if (await _access.CanManageAsync(offer.VenueId, userId))
                return (offer, false, null);

            return (null, false, new ErrorResult("Bu yazışma sizə aid deyil."));
        }

        private static string OfferStatusLabel(VenueOfferStatus status) => status switch
        {
            VenueOfferStatus.Sent => "Göndərilib",
            VenueOfferStatus.Accepted => "Qəbul edilib",
            VenueOfferStatus.Declined => "Uyğun gəlmədi",
            _ => "Geri götürülüb"
        };
    }
}
