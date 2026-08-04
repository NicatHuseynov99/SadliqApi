using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos.Reservation;
using Microsoft.EntityFrameworkCore;

namespace Business.Concrete
{
    public class ReservationManager : IReservationService
    {
        private readonly IReservationDal _reservationDal;
        private readonly IHallAvailabilityDal _availabilityDal;
        private readonly IVenueDal _venueDal;
        private readonly IHallDal _hallDal;
        private readonly IMenuDal _menuDal;
        private readonly IVenueAccessService _access;
        private readonly IUserDal _userDal;
        private readonly INotificationService _notifications;
        private readonly IAuditContext _audit;
        private readonly IPlatformSettingsService _settings;
        private readonly IReservationCounterOfferDal _counterOfferDal;

        // Tutulmanın ilkin müddəti — admin paneldən dəyişdirilə bilər
        private const int DefaultHoldHours = 48;

        // Bir müştərinin eyni anda cavab gözləyən sorğularının sayı
        private const int MaxOpenRequestsPerCustomer = 3;

        public ReservationManager(
            IReservationDal reservationDal,
            IHallAvailabilityDal availabilityDal,
            IVenueDal venueDal,
            IHallDal hallDal,
            IMenuDal menuDal,
            IVenueAccessService access,
            IUserDal userDal,
            INotificationService notifications,
            IAuditContext audit,
            IPlatformSettingsService settings,
            IReservationCounterOfferDal counterOfferDal)
        {
            _counterOfferDal = counterOfferDal;
            _audit = audit;
            _settings = settings;
            _reservationDal = reservationDal;
            _availabilityDal = availabilityDal;
            _venueDal = venueDal;
            _hallDal = hallDal;
            _menuDal = menuDal;
            _access = access;
            _userDal = userDal;
            _notifications = notifications;
        }

        // Zal + gün + menyu seçib rezerv sorğusu göndərir.
        // Bütün yoxlamalar server tərəfdə aparılır — frontend məhdudiyyətlərinə etibar edilmir.
        public async Task<IDataResult<Reservation>> CreateAsync(CreateReservationDto dto, int customerId)
        {
            // Tədbir tarixi keçmişdə və ya bu gün ola bilməz
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            if (dto.EventDate <= today)
                return new ErrorDataResult<Reservation>("Tədbir tarixi bugündən sonrakı gün olmalıdır.");

            // Saray mövcud və təsdiqlənmiş olmalıdır
            var venue = await _venueDal.GetAsync(v => v.Id == dto.VenueId);
            if (venue is null)
                return new ErrorDataResult<Reservation>("Saray tapılmadı.");

            if (!venue.IsApproved)
                return new ErrorDataResult<Reservation>("Bu saray hələ təsdiqlənməyib.");

            // Zal həmin saraya aid olmalıdır
            var hall = await _hallDal.GetAsync(h => h.Id == dto.HallId);
            if (hall is null || hall.VenueId != dto.VenueId)
                return new ErrorDataResult<Reservation>("Seçilən zal bu saraya aid deyil.");

            // Menyu da həmin saraya aid olmalıdır
            var menu = await _menuDal.GetAsync(m => m.Id == dto.MenuId);
            if (menu is null || menu.VenueId != dto.VenueId)
                return new ErrorDataResult<Reservation>("Seçilən menyu bu saraya aid deyil.");

            // Qonaq sayı zalın tutumunu aşa bilməz
            if (dto.GuestCount < 1)
                return new ErrorDataResult<Reservation>("Qonaq sayı ən azı 1 olmalıdır.");

            if (dto.GuestCount > hall.Capacity)
                return new ErrorDataResult<Reservation>(
                    $"Bu zalın tutumu {hall.Capacity} nəfərdir, {dto.GuestCount} qonaq yerləşdirmək mümkün deyil.");

            if (string.IsNullOrWhiteSpace(dto.ContactPhone))
                return new ErrorDataResult<Reservation>("Əlaqə telefonu boş ola bilməz.");

            // Bir müştəri eyni anda çox tarix bağlaya bilməz — cavabsız sorğularla
            // sarayın təqvimini doldurmağın qarşısını alır
            var openQuery = await _reservationDal.GetListAsync(
                r => r.CustomerId == customerId && r.Status == ReservationStatus.Pending);

            var openCount = await openQuery.CountAsync();
            if (openCount >= MaxOpenRequestsPerCustomer)
                return new ErrorDataResult<Reservation>(
                    $"Eyni vaxtda ən çox {MaxOpenRequestsPerCustomer} açıq sorğunuz ola bilər. " +
                    "Cavab gözləyən sorğulardan birini ləğv edin.");

            // Eyni zala eyni tarixə təkrar sorğu göndərilməsin
            var duplicate = await _reservationDal.GetAsync(
                r => r.CustomerId == customerId
                     && r.HallId == dto.HallId
                     && r.EventDate == dto.EventDate
                     && r.Status == ReservationStatus.Pending);

            if (duplicate is not null)
                return new ErrorDataResult<Reservation>("Bu zala həmin tarix üçün artıq sorğu göndərmisiniz.");

            var existingAvailability = await _availabilityDal.GetAsync(
                a => a.HallId == dto.HallId && a.Date == dto.EventDate);

            if (existingAvailability != null && existingAvailability.Status != AvailabilityStatus.Available)
                return new ErrorDataResult<Reservation>("Bu zal seçdiyiniz tarixdə artıq doludur.");

            var reservation = new Reservation
            {
                CustomerId = customerId,
                VenueId = dto.VenueId,
                HallId = dto.HallId,
                MenuId = dto.MenuId,
                EventDate = dto.EventDate,
                GuestCount = dto.GuestCount,
                ContactPhone = dto.ContactPhone,
                Note = dto.Note,
                Status = ReservationStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _reservationDal.AddAsync(reservation);

            // Sorğu göndərilən kimi günü "tutulmuş" kimi işarələyirik ki, başqası eyni günə
            // sorğu göndərməsin. Tutulma müddətlidir: sahib cavab verməsə özü boşalır.
            var holdHours = await _settings.GetIntAsync("reservation_hold_hours", DefaultHoldHours);
            var holdUntil = DateTime.UtcNow.AddHours(holdHours);

            if (existingAvailability == null)
            {
                await _availabilityDal.AddAsync(new HallAvailability
                {
                    HallId = dto.HallId,
                    Date = dto.EventDate,
                    Status = AvailabilityStatus.Held,
                    HoldExpiresAt = holdUntil
                });
            }
            else
            {
                existingAvailability.Status = AvailabilityStatus.Held;
                existingAvailability.HoldExpiresAt = holdUntil;
                await _availabilityDal.UpdateAsync(existingAvailability);
            }

            // Saray sahibinə yeni sorğu barədə xəbər verilir (e-poçt konfiqurasiya olunmayıbsa log-a düşür)
            await NotifyOwnerAsync(venue, hall, menu, reservation);

            _audit.Set(
                "ReservationCreated",
                $"«{venue.Name}» sarayının «{hall.Name}» zalına {dto.EventDate:dd.MM.yyyy} tarixi üçün {dto.GuestCount} qonaqlıq rezerv sorğusu göndərdi",
                "Reservation", reservation.Id, venue.Id);

            return new SuccessDataResult<Reservation>(reservation, "Rezerv sorğusu göndərildi.");
        }

        // Sahibə yeni sorğu barədə bildiriş. Kanalları bildiriş servisi seçir —
        // burada yalnız mətn qurulur.
        private async Task NotifyOwnerAsync(Venue venue, Hall hall, Menu menu, Reservation reservation)
        {
            var body =
                $"«{venue.Name}» üçün yeni sorğu daxil oldu.\n" +
                $"Zal: {hall.Name}\n" +
                $"Tarix: {reservation.EventDate:dd.MM.yyyy}\n" +
                $"Qonaq sayı: {reservation.GuestCount}\n" +
                $"Menyu: {menu.Name} ({menu.PricePerPerson} ₼ / nəfər)\n" +
                $"Əlaqə: {reservation.ContactPhone}\n\n" +
                "Sorğunu idarəetmə panelindən təsdiqləyə və ya rədd edə bilərsiniz.";

            await _notifications.NotifyAsync(
                venue.OwnerId,
                new NotificationMessage(
                    NotificationType.ReservationCreated,
                    $"Yeni rezerv sorğusu — {venue.Name}",
                    body,
                    "/panel"));
        }

        // Müştəri ləğv edəndə sahibə xəbər — tarix yenidən boşdur
        private async Task NotifyOwnerOfCancellationAsync(Reservation reservation)
        {
            var venue = await _venueDal.GetAsync(v => v.Id == reservation.VenueId);
            if (venue is null) return;

            var hall = await _hallDal.GetAsync(h => h.Id == reservation.HallId);

            await _notifications.NotifyAsync(
                venue.OwnerId,
                new NotificationMessage(
                    NotificationType.ReservationCancelled,
                    $"Rezerv ləğv edildi — {venue.Name}",
                    $"«{hall?.Name ?? "Zal"}» — {reservation.EventDate:dd.MM.yyyy} tarixi üçün sorğu " +
                    "müştəri tərəfindən ləğv olundu. Həmin gün yenidən boşdur.",
                    "/panel"));
        }

        // Müştəriyə sorğunun nəticəsi barədə bildiriş
        private async Task NotifyCustomerAsync(Reservation reservation, ReservationStatus status)
        {
            var venue = await _venueDal.GetAsync(v => v.Id == reservation.VenueId);
            var venueName = venue?.Name ?? "Saray";
            var date = reservation.EventDate.ToString("dd.MM.yyyy");

            var (title, message, type) = status switch
            {
                ReservationStatus.Confirmed => (
                    "Rezerviniz təsdiqləndi",
                    $"«{venueName}» üçün {date} tarixinə göndərdiyiniz sorğu təsdiqləndi. Təbrik edirik!",
                    NotificationType.ReservationStatusChanged),

                ReservationStatus.Rejected => (
                    "Rezerv sorğunuz rədd edildi",
                    $"«{venueName}» üçün {date} tarixinə göndərdiyiniz sorğu təəssüf ki qəbul olunmadı. " +
                    "Başqa tarix və ya saray seçə bilərsiniz.",
                    NotificationType.ReservationStatusChanged),

                ReservationStatus.Expired => (
                    "Rezerv sorğunuz vaxtı keçdi",
                    $"«{venueName}» üçün {date} tarixinə göndərdiyiniz sorğuya vaxtında cavab verilmədi " +
                    "və sorğu bağlandı. Sarayla birbaşa əlaqə saxlaya bilərsiniz.",
                    NotificationType.ReservationExpired),

                _ => (string.Empty, string.Empty, NotificationType.SystemMessage)
            };

            if (string.IsNullOrEmpty(title)) return;

            await _notifications.NotifyAsync(
                reservation.CustomerId,
                new NotificationMessage(type, title, message, "/rezervlerim"));
        }

        // Müştərinin öz göndərdiyi bütün rezerv sorğularının siyahısı (status izləmək üçün)
        public async Task<IDataResult<List<Reservation>>> GetByCustomerAsync(int customerId)
        {
            var query = await _reservationDal.GetListAsync(r => r.CustomerId == customerId);
            var list = await query
                .Include(r => r.Venue)
                .Include(r => r.Hall)
                .Include(r => r.Menu)
                .Include(r => r.CounterOffers)!.ThenInclude(o => o.Hall)
                .Include(r => r.CounterOffers)!.ThenInclude(o => o.Menu)
                .ToListAsync();

            return new SuccessDataResult<List<Reservation>>(list);
        }

        // Sarayın rezerv sorğuları — sahib və ya həmin saraya təyin olunmuş administrator görə bilər
        public async Task<IDataResult<List<Reservation>>> GetByVenueAsync(int venueId, int ownerId)
        {
            if (!await _access.CanModifyAsync(venueId, ownerId))
                return new ErrorDataResult<List<Reservation>>("Bu saray sizə aid deyil.");

            var allowedHalls = await _access.GetAllowedHallIdsAsync(ownerId);

            var query = await _reservationDal.GetListAsync(r => r.VenueId == venueId);

            if (allowedHalls is not null)
                query = query.Where(r => allowedHalls.Contains(r.HallId));

            var list = await query
                .Include(r => r.Customer)
                .Include(r => r.Hall)
                .Include(r => r.Menu)
                .Include(r => r.CounterOffers)
                .ToListAsync();

            return new SuccessDataResult<List<Reservation>>(list);
        }

        // İdarə edilən bütün sarayların rezervləri bir sorğu ilə (panel üçün).
        // Sahib öz saraylarını, administrator isə yalnız təyin olunduğu sarayları görür.
        public async Task<IDataResult<List<Reservation>>> GetByOwnerAsync(int ownerId)
        {
            var venueIds = await _access.GetManagedVenueIdsAsync(ownerId);
            var allowedHalls = await _access.GetAllowedHallIdsAsync(ownerId);

            var query = await _reservationDal.GetListAsync(r => venueIds.Contains(r.VenueId));

            // Administratora yalnız icazəli zalların sorğuları göstərilir
            if (allowedHalls is not null)
                query = query.Where(r => allowedHalls.Contains(r.HallId));

            var list = await query
                .Include(r => r.Customer)
                .Include(r => r.Hall)
                .Include(r => r.Menu)
                .Include(r => r.CounterOffers)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return new SuccessDataResult<List<Reservation>>(list);
        }

        // Owner rezervi təsdiq/rədd edir; təsdiq olunanda gün "Booked", rədd olunanda yenidən "Available" olur
        public async Task<IResult> UpdateStatusAsync(int reservationId, UpdateReservationStatusDto dto, int ownerId)
        {
            var reservation = await _reservationDal.GetAsync(r => r.Id == reservationId);
            if (reservation is null)
                return new ErrorResult("Rezerv tapılmadı.");

            // Zal səviyyəsində yoxlama: administrator yalnız öz zallarının sorğularına cavab verir
            if (!await _access.CanModifyHallAsync(reservation.HallId, ownerId))
                return new ErrorResult("Bu rezerv sizə aid deyil.");

            return await ApplyStatusAsync(reservation, dto.Status);
        }

        // Platforma admini sahibin icazəsi olmadan da statusu dəyişə bilər —
        // mübahisəli hallarda müdaxilə etmək lazım gəlir. Nəticə eyni cür işlənir.
        public async Task<IResult> AdminUpdateStatusAsync(int reservationId, UpdateReservationStatusDto dto)
        {
            var reservation = await _reservationDal.GetAsync(r => r.Id == reservationId);
            if (reservation is null)
                return new ErrorResult("Rezerv tapılmadı.");

            if (reservation.Status == dto.Status)
                return new ErrorResult("Rezerv artıq bu vəziyyətdədir.");

            return await ApplyStatusAsync(reservation, dto.Status);
        }

        // Status dəyişikliyinin bütün nəticələri: təqvim, bildiriş, jurnal
        private async Task<IResult> ApplyStatusAsync(Reservation reservation, ReservationStatus status)
        {
            var dto = new UpdateReservationStatusDto { Status = status };
            reservation.Status = dto.Status;
            await _reservationDal.UpdateAsync(reservation);

            var availability = await _availabilityDal.GetAsync(
                a => a.HallId == reservation.HallId && a.Date == reservation.EventDate);

            if (availability != null)
            {
                availability.Status = dto.Status switch
                {
                    ReservationStatus.Confirmed => AvailabilityStatus.Booked,
                    ReservationStatus.Rejected
                        or ReservationStatus.Cancelled
                        or ReservationStatus.Expired => AvailabilityStatus.Available,
                    _ => availability.Status
                };

                // Cavab verildi — müddət sahəsi artıq lazım deyil
                availability.HoldExpiresAt = null;
                await _availabilityDal.UpdateAsync(availability);
            }

            // Müştəriyə nəticə barədə xəbər verilir
            await NotifyCustomerAsync(reservation, dto.Status);

            // Jurnal üçün dəqiq izah
            var venueName = (await _venueDal.GetAsync(v => v.Id == reservation.VenueId))?.Name ?? "saray";
            var statusText = dto.Status switch
            {
                ReservationStatus.Confirmed => "təsdiqlədi",
                ReservationStatus.Rejected => "rədd etdi",
                ReservationStatus.Cancelled => "ləğv etdi",
                _ => "gözləmə vəziyyətinə qaytardı"
            };

            _audit.Set(
                "ReservationStatusChanged",
                $"«{venueName}» üçün {reservation.EventDate:dd.MM.yyyy} tarixli rezerv sorğusunu {statusText}",
                "Reservation", reservation.Id, reservation.VenueId);

            return new SuccessResult("Status yeniləndi.");
        }

        // Müştərinin öz göndərdiyi rezerv sorğusunu ləğv etməsi; günü yenidən boş edir
        public async Task<IResult> CancelAsync(int reservationId, int customerId)
        {
            var reservation = await _reservationDal.GetAsync(r => r.Id == reservationId && r.CustomerId == customerId);
            if (reservation is null)
                return new ErrorResult("Rezerv tapılmadı və ya sizə aid deyil.");

            reservation.Status = ReservationStatus.Cancelled;
            await _reservationDal.UpdateAsync(reservation);

            var availability = await _availabilityDal.GetAsync(
                a => a.HallId == reservation.HallId && a.Date == reservation.EventDate);

            if (availability != null)
            {
                availability.Status = AvailabilityStatus.Available;
                availability.HoldExpiresAt = null;
                await _availabilityDal.UpdateAsync(availability);
            }

            // Sahib boş qalan tarixdən xəbər tutmalıdır — yoxsa günü satmaq şansını itirir
            await NotifyOwnerOfCancellationAsync(reservation);

            _audit.Set(
                "ReservationCancelled",
                $"{reservation.EventDate:dd.MM.yyyy} tarixli rezerv sorğusunu ləğv etdi",
                "Reservation", reservation.Id, reservation.VenueId);

            return new SuccessResult("Rezerv ləğv edildi.");
        }

        // ————— əks-təklif —————

        // Saray sorğunu açdı. Müştəri «baxıldı» görsün deyə bir dəfə qeyd olunur.
        //
        // Platforma işçisinin (admin, dəstək) baxışı sayılmır: müştəri üçün
        // «saray baxıb» yazısı sarayın özünün xəbər tutduğu mənasını verməlidir.
        public async Task<IResult> MarkSeenAsync(int reservationId, int ownerId)
        {
            var reservation = await _reservationDal.GetAsync(r => r.Id == reservationId);
            if (reservation is null) return new ErrorResult("Rezerv tapılmadı.");

            if (!await _access.CanModifyHallAsync(reservation.HallId, ownerId))
                return new ErrorResult("Bu rezerv sizə aid deyil.");

            var viewer = await _userDal.GetAsync(u => u.Id == ownerId);
            if (viewer is null || viewer.Role is UserRole.SuperAdmin or UserRole.Support)
                return new SuccessResult();

            if (reservation.SeenAt is not null) return new SuccessResult();

            reservation.SeenAt = DateTime.UtcNow;
            reservation.SeenByUserId = ownerId;
            await _reservationDal.UpdateAsync(reservation);

            return new SuccessResult();
        }

        // «Rədd et» əvəzinə başqa şərt təklif etmək. Rədd edilən sorğuların
        // böyük hissəsi əslində «o gün dolu, başqa gün var» deməkdir.
        public async Task<IDataResult<ReservationCounterOffer>> CreateCounterOfferAsync(
            CreateCounterOfferDto dto, int ownerId)
        {
            var reservation = await _reservationDal.GetAsync(r => r.Id == dto.ReservationId);
            if (reservation is null)
                return new ErrorDataResult<ReservationCounterOffer>("Rezerv tapılmadı.");

            if (!await _access.CanModifyHallAsync(reservation.HallId, ownerId))
                return new ErrorDataResult<ReservationCounterOffer>("Bu rezerv sizə aid deyil.");

            if (reservation.Status is not (ReservationStatus.Pending or ReservationStatus.CounterOffered))
                return new ErrorDataResult<ReservationCounterOffer>(
                    "Yalnız cavab gözləyən sorğuya əks-təklif göndərmək olar.");

            // Ən azı bir şərt dəyişməlidir — yoxsa təklifin mənası yoxdur
            var changesDate = dto.EventDate.HasValue && dto.EventDate != reservation.EventDate;
            var changesHall = dto.HallId.HasValue && dto.HallId != reservation.HallId;
            var changesMenu = dto.MenuId.HasValue && dto.MenuId != reservation.MenuId;
            var changesPrice = dto.PricePerPerson.HasValue && dto.PricePerPerson > 0;

            if (!changesDate && !changesHall && !changesMenu && !changesPrice)
                return new ErrorDataResult<ReservationCounterOffer>(
                    "Ən azı bir şərt dəyişməlidir: tarix, zal, menyu və ya qiymət.");

            if (string.IsNullOrWhiteSpace(dto.Message) || dto.Message.Trim().Length < 5)
                return new ErrorDataResult<ReservationCounterOffer>(
                    "Müştəriyə qısa izah yazın — niyə bu təklifi göndərirsiniz.");

            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            if (changesDate && dto.EventDate <= today)
                return new ErrorDataResult<ReservationCounterOffer>(
                    "Təklif olunan tarix bugündən sonrakı gün olmalıdır.");

            // Təklif olunan zal eyni saraya aid olmalıdır
            var targetHallId = dto.HallId ?? reservation.HallId;
            var hall = await _hallDal.GetAsync(h => h.Id == targetHallId);
            if (hall is null || hall.VenueId != reservation.VenueId)
                return new ErrorDataResult<ReservationCounterOffer>("Seçilən zal bu saraya aid deyil.");

            if (reservation.GuestCount > hall.Capacity)
                return new ErrorDataResult<ReservationCounterOffer>(
                    $"«{hall.Name}» zalının tutumu {hall.Capacity} nəfərdir, " +
                    $"müştəri isə {reservation.GuestCount} qonaq gözləyir.");

            if (dto.MenuId.HasValue)
            {
                var menu = await _menuDal.GetAsync(m => m.Id == dto.MenuId.Value);
                if (menu is null || menu.VenueId != reservation.VenueId)
                    return new ErrorDataResult<ReservationCounterOffer>("Seçilən menyu bu saraya aid deyil.");
            }

            var targetDate = dto.EventDate ?? reservation.EventDate;

            // Təklif olunan gün başqasına satılmasın deyə o da tutulur
            var target = await _availabilityDal.GetAsync(
                a => a.HallId == targetHallId && a.Date == targetDate);

            var sameSlot = targetHallId == reservation.HallId && targetDate == reservation.EventDate;

            if (target is not null && target.Status != AvailabilityStatus.Available && !sameSlot)
                return new ErrorDataResult<ReservationCounterOffer>(
                    "Təklif etdiyiniz gün həmin zalda artıq doludur.");

            var holdHours = await _settings.GetIntAsync("reservation_hold_hours", DefaultHoldHours);
            var holdUntil = DateTime.UtcNow.AddHours(holdHours);

            if (target is null)
            {
                await _availabilityDal.AddAsync(new HallAvailability
                {
                    HallId = targetHallId,
                    Date = targetDate,
                    Status = AvailabilityStatus.Held,
                    HoldExpiresAt = holdUntil
                });
            }
            else
            {
                target.Status = AvailabilityStatus.Held;
                target.HoldExpiresAt = holdUntil;
                await _availabilityDal.UpdateAsync(target);
            }

            // İlkin tarixin tutulması da uzadılır: müştəri cavab verənə qədər
            // köhnə variant əldən çıxmasın
            if (!sameSlot)
            {
                var original = await _availabilityDal.GetAsync(
                    a => a.HallId == reservation.HallId && a.Date == reservation.EventDate);

                if (original is not null && original.Status == AvailabilityStatus.Held)
                {
                    original.HoldExpiresAt = holdUntil;
                    await _availabilityDal.UpdateAsync(original);
                }
            }

            // Cavabsız qalan əvvəlki təklif geri götürülür — eyni anda bir təklif olur
            var previousQuery = await _counterOfferDal.GetListAsync(
                o => o.ReservationId == reservation.Id && o.Status == CounterOfferStatus.Pending);

            foreach (var previous in await previousQuery.ToListAsync())
            {
                var tracked = await _counterOfferDal.GetAsync(o => o.Id == previous.Id);
                if (tracked is null) continue;

                tracked.Status = CounterOfferStatus.Withdrawn;
                tracked.RespondedAt = DateTime.UtcNow;
                await _counterOfferDal.UpdateAsync(tracked);
            }

            var offer = new ReservationCounterOffer
            {
                ReservationId = reservation.Id,
                CreatedByUserId = ownerId,
                EventDate = changesDate ? dto.EventDate : null,
                HallId = changesHall ? dto.HallId : null,
                MenuId = changesMenu ? dto.MenuId : null,
                PricePerPerson = changesPrice ? dto.PricePerPerson : null,
                Message = dto.Message.Trim(),
                Status = CounterOfferStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _counterOfferDal.AddAsync(offer);

            reservation.Status = ReservationStatus.CounterOffered;

            // Təklif göndərən sarayın adamıdırsa, bu həm də «baxıldı» deməkdir
            var author = await _userDal.GetAsync(u => u.Id == ownerId);
            if (author is not null && author.Role is not (UserRole.SuperAdmin or UserRole.Support)
                && reservation.SeenAt is null)
            {
                reservation.SeenAt = DateTime.UtcNow;
                reservation.SeenByUserId = ownerId;
            }

            await _reservationDal.UpdateAsync(reservation);

            var venue = await _venueDal.GetAsync(v => v.Id == reservation.VenueId);
            var summary = await DescribeOfferAsync(reservation, offer);

            await _notifications.NotifyAsync(
                reservation.CustomerId,
                new NotificationMessage(
                    NotificationType.ReservationStatusChanged,
                    $"«{venue?.Name ?? "Saray"}» yeni şərt təklif etdi",
                    summary + "\n\n" + offer.Message,
                    "/rezervlerim"));

            _audit.Set(
                "ReservationCounterOffered",
                $"{reservation.EventDate:dd.MM.yyyy} tarixli sorğuya əks-təklif göndərdi",
                "Reservation", reservation.Id, reservation.VenueId);

            return new SuccessDataResult<ReservationCounterOffer>(offer, "Əks-təklif müştəriyə göndərildi.");
        }

        // Müştəri qəbul etdi: şərtlər rezervə köçürülür və rezerv təsdiqlənir
        public async Task<IResult> AcceptCounterOfferAsync(int offerId, int customerId)
        {
            var (offer, reservation, error) = await LoadOfferAsync(offerId, customerId);
            if (error is not null) return error;

            var oldHallId = reservation!.HallId;
            var oldDate = reservation.EventDate;

            if (offer!.EventDate.HasValue) reservation.EventDate = offer.EventDate.Value;
            if (offer.HallId.HasValue) reservation.HallId = offer.HallId.Value;
            if (offer.MenuId.HasValue) reservation.MenuId = offer.MenuId.Value;

            reservation.Status = ReservationStatus.Confirmed;
            await _reservationDal.UpdateAsync(reservation);

            offer.Status = CounterOfferStatus.Accepted;
            offer.RespondedAt = DateTime.UtcNow;
            await _counterOfferDal.UpdateAsync(offer);

            // Yeni gün tutulur, köhnə gün (dəyişibsə) boşalır
            await SetAvailabilityAsync(reservation.HallId, reservation.EventDate, AvailabilityStatus.Booked);

            if (oldHallId != reservation.HallId || oldDate != reservation.EventDate)
                await SetAvailabilityAsync(oldHallId, oldDate, AvailabilityStatus.Available);

            var venue = await _venueDal.GetAsync(v => v.Id == reservation.VenueId);
            var customer = await _userDal.GetAsync(u => u.Id == reservation.CustomerId);

            if (venue is not null)
                await _notifications.NotifyAsync(
                    venue.OwnerId,
                    new NotificationMessage(
                        NotificationType.ReservationStatusChanged,
                        "Əks-təklif qəbul edildi",
                        $"{customer?.FullName ?? "Müştəri"} təklifinizi qəbul etdi. " +
                        $"Rezerv təsdiqləndi: {reservation.EventDate:dd.MM.yyyy}.",
                        "/panel"));

            _audit.Set(
                "CounterOfferAccepted",
                $"{reservation.EventDate:dd.MM.yyyy} tarixli əks-təklifi qəbul etdi",
                "Reservation", reservation.Id, reservation.VenueId);

            return new SuccessResult("Təklif qəbul edildi, rezerviniz təsdiqləndi.");
        }

        // Müştəri rədd etdi: sorğu bağlanır, tutulmuş günlər boşalır
        public async Task<IResult> DeclineCounterOfferAsync(int offerId, int customerId)
        {
            var (offer, reservation, error) = await LoadOfferAsync(offerId, customerId);
            if (error is not null) return error;

            offer!.Status = CounterOfferStatus.Declined;
            offer.RespondedAt = DateTime.UtcNow;
            await _counterOfferDal.UpdateAsync(offer);

            reservation!.Status = ReservationStatus.Rejected;
            await _reservationDal.UpdateAsync(reservation);

            await SetAvailabilityAsync(reservation.HallId, reservation.EventDate, AvailabilityStatus.Available);

            if (offer.EventDate.HasValue || offer.HallId.HasValue)
                await SetAvailabilityAsync(
                    offer.HallId ?? reservation.HallId,
                    offer.EventDate ?? reservation.EventDate,
                    AvailabilityStatus.Available);

            var venue = await _venueDal.GetAsync(v => v.Id == reservation.VenueId);

            if (venue is not null)
                await _notifications.NotifyAsync(
                    venue.OwnerId,
                    new NotificationMessage(
                        NotificationType.ReservationStatusChanged,
                        "Əks-təklif qəbul edilmədi",
                        $"{reservation.EventDate:dd.MM.yyyy} tarixi üçün göndərdiyiniz təklif " +
                        "müştəri tərəfindən rədd edildi. Tarixlər yenidən boşdur.",
                        "/panel"));

            return new SuccessResult("Təklif rədd edildi.");
        }

        // Təklifi tapır və müştəriyə aid olduğunu yoxlayır
        private async Task<(ReservationCounterOffer? Offer, Reservation? Reservation, IResult? Error)>
            LoadOfferAsync(int offerId, int customerId)
        {
            var offer = await _counterOfferDal.GetAsync(o => o.Id == offerId);
            if (offer is null)
                return (null, null, new ErrorResult("Təklif tapılmadı."));

            if (offer.Status != CounterOfferStatus.Pending)
                return (null, null, new ErrorResult("Bu təklifə artıq cavab verilib."));

            var reservation = await _reservationDal.GetAsync(r => r.Id == offer.ReservationId);
            if (reservation is null || reservation.CustomerId != customerId)
                return (null, null, new ErrorResult("Bu təklif sizə aid deyil."));

            return (offer, reservation, null);
        }

        private async Task SetAvailabilityAsync(int hallId, DateOnly date, AvailabilityStatus status)
        {
            var slot = await _availabilityDal.GetAsync(a => a.HallId == hallId && a.Date == date);

            if (slot is null)
            {
                if (status == AvailabilityStatus.Available) return;

                await _availabilityDal.AddAsync(new HallAvailability
                {
                    HallId = hallId,
                    Date = date,
                    Status = status
                });
                return;
            }

            slot.Status = status;
            slot.HoldExpiresAt = null;
            await _availabilityDal.UpdateAsync(slot);
        }

        // Təklifin nəyi dəyişdiyini insan dilində yazır
        private async Task<string> DescribeOfferAsync(Reservation reservation, ReservationCounterOffer offer)
        {
            var lines = new List<string>();

            if (offer.EventDate.HasValue)
                lines.Add($"Tarix: {reservation.EventDate:dd.MM.yyyy} → {offer.EventDate:dd.MM.yyyy}");

            if (offer.HallId.HasValue)
            {
                var oldHall = await _hallDal.GetAsync(h => h.Id == reservation.HallId);
                var newHall = await _hallDal.GetAsync(h => h.Id == offer.HallId.Value);
                lines.Add($"Zal: {oldHall?.Name} → {newHall?.Name}");
            }

            if (offer.MenuId.HasValue)
            {
                var oldMenu = await _menuDal.GetAsync(m => m.Id == reservation.MenuId);
                var newMenu = await _menuDal.GetAsync(m => m.Id == offer.MenuId.Value);
                lines.Add($"Menyu: {oldMenu?.Name} → {newMenu?.Name}");
            }

            if (offer.PricePerPerson.HasValue)
                lines.Add($"Qiymət: {offer.PricePerPerson} ₼ / nəfər");

            return string.Join("\n", lines);
        }
    }
}
