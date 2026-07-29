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

        public ReservationManager(
            IReservationDal reservationDal,
            IHallAvailabilityDal availabilityDal,
            IVenueDal venueDal,
            IHallDal hallDal,
            IMenuDal menuDal,
            IVenueAccessService access)
        {
            _reservationDal = reservationDal;
            _availabilityDal = availabilityDal;
            _venueDal = venueDal;
            _hallDal = hallDal;
            _menuDal = menuDal;
            _access = access;
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

            return new SuccessDataResult<Reservation>(reservation, "Rezerv sorğusu göndərildi.");
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
            if (!await _access.CanManageAsync(venueId, ownerId))
                return new ErrorDataResult<List<Reservation>>("Bu saray sizə aid deyil.");

            var query = await _reservationDal.GetListAsync(r => r.VenueId == venueId);
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

            var query = await _reservationDal.GetListAsync(r => venueIds.Contains(r.VenueId));
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

            if (!await _access.CanManageAsync(reservation.VenueId, ownerId))
                return new ErrorResult("Bu rezerv sizə aid deyil.");

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
