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
        private readonly IEmailService _emailService;
        private readonly IAuditContext _audit;

        public ReservationManager(
            IReservationDal reservationDal,
            IHallAvailabilityDal availabilityDal,
            IVenueDal venueDal,
            IHallDal hallDal,
            IMenuDal menuDal,
            IVenueAccessService access,
            IUserDal userDal,
            IEmailService emailService,
            IAuditContext audit)
        {
            _audit = audit;
            _reservationDal = reservationDal;
            _availabilityDal = availabilityDal;
            _venueDal = venueDal;
            _hallDal = hallDal;
            _menuDal = menuDal;
            _access = access;
            _userDal = userDal;
            _emailService = emailService;
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

            // Sorğu göndərilən kimi günü "tutulmuş" kimi işarələyirik ki, başqası eyni günə sorğu göndərməsin
            if (existingAvailability == null)
            {
                await _availabilityDal.AddAsync(new HallAvailability
                {
                    HallId = dto.HallId,
                    Date = dto.EventDate,
                    Status = AvailabilityStatus.Held
                });
            }
            else
            {
                existingAvailability.Status = AvailabilityStatus.Held;
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

        // Sahibə yeni rezerv sorğusu barədə məktub
        private async Task NotifyOwnerAsync(Venue venue, Hall hall, Menu menu, Reservation reservation)
        {
            var owner = await _userDal.GetAsync(u => u.Id == venue.OwnerId);
            if (owner is null || string.IsNullOrWhiteSpace(owner.Email))
                return;

            var body = $@"
                <div style='font-family:Segoe UI,Arial,sans-serif;color:#1a1118'>
                  <h2 style='color:#721438'>Yeni rezerv sorğusu</h2>
                  <p><b>{venue.Name}</b> üçün yeni sorğu daxil oldu.</p>
                  <ul>
                    <li>Zal: {hall.Name}</li>
                    <li>Tarix: {reservation.EventDate:dd.MM.yyyy}</li>
                    <li>Qonaq sayı: {reservation.GuestCount}</li>
                    <li>Menyu: {menu.Name} ({menu.PricePerPerson} ₼ / nəfər)</li>
                    <li>Əlaqə: {reservation.ContactPhone}</li>
                  </ul>
                  <p>Sorğunu idarəetmə panelindən təsdiqləyə və ya rədd edə bilərsiniz.</p>
                </div>";

            await _emailService.SendAsync(owner.Email, $"Yeni rezerv sorğusu — {venue.Name}", body);
        }

        // Müştəriyə sorğunun nəticəsi barədə məktub
        private async Task NotifyCustomerAsync(Reservation reservation, ReservationStatus status)
        {
            var customer = await _userDal.GetAsync(u => u.Id == reservation.CustomerId);
            if (customer is null || string.IsNullOrWhiteSpace(customer.Email))
                return;

            var venue = await _venueDal.GetAsync(v => v.Id == reservation.VenueId);
            var venueName = venue?.Name ?? "Saray";

            var (title, message) = status switch
            {
                ReservationStatus.Confirmed => ("Rezerviniz təsdiqləndi",
                    $"<b>{venueName}</b> üçün {reservation.EventDate:dd.MM.yyyy} tarixinə göndərdiyiniz sorğu təsdiqləndi. Təbrik edirik!"),
                ReservationStatus.Rejected => ("Rezerv sorğunuz rədd edildi",
                    $"<b>{venueName}</b> üçün {reservation.EventDate:dd.MM.yyyy} tarixinə göndərdiyiniz sorğu təəssüf ki qəbul olunmadı. Başqa tarix və ya saray seçə bilərsiniz."),
                _ => (string.Empty, string.Empty)
            };

            if (string.IsNullOrEmpty(title))
                return;

            var body = $@"
                <div style='font-family:Segoe UI,Arial,sans-serif;color:#1a1118'>
                  <h2 style='color:#721438'>{title}</h2>
                  <p>{message}</p>
                  <p style='color:#868e96;font-size:13px'>Şadlıq Sarayları</p>
                </div>";

            await _emailService.SendAsync(customer.Email, title, body);
        }

        // Müştərinin öz göndərdiyi bütün rezerv sorğularının siyahısı (status izləmək üçün)
        public async Task<IDataResult<List<Reservation>>> GetByCustomerAsync(int customerId)
        {
            var query = await _reservationDal.GetListAsync(r => r.CustomerId == customerId);
            var list = await query
                .Include(r => r.Venue)
                .Include(r => r.Hall)
                .Include(r => r.Menu)
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
                    ReservationStatus.Rejected or ReservationStatus.Cancelled => AvailabilityStatus.Available,
                    _ => availability.Status
                };
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
                await _availabilityDal.UpdateAsync(availability);
            }

            return new SuccessResult("Rezerv ləğv edildi.");
        }
    }
}
