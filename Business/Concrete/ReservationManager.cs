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

        public ReservationManager(IReservationDal reservationDal, IHallAvailabilityDal availabilityDal, IVenueDal venueDal)
        {
            _reservationDal = reservationDal;
            _availabilityDal = availabilityDal;
            _venueDal = venueDal;
        }

        // Zal + gün + menyu seçib rezerv sorğusu göndərir; eyni gün üçün ikiqat rezerv qadağandır
        public async Task<IDataResult<Reservation>> CreateAsync(CreateReservationDto dto, int customerId)
        {
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

        // Restoran sahibinin öz sarayına gələn rezerv sorğularını görməsi
        public async Task<IDataResult<List<Reservation>>> GetByVenueAsync(int venueId, int ownerId)
        {
            var venue = await _venueDal.GetAsync(v => v.Id == venueId && v.OwnerId == ownerId);
            if (venue is null)
                return new ErrorDataResult<List<Reservation>>("Bu saray sizə aid deyil.");

            var query = await _reservationDal.GetListAsync(r => r.VenueId == venueId);
            var list = await query
                .Include(r => r.Customer)
                .Include(r => r.Hall)
                .Include(r => r.Menu)
                .ToListAsync();

            return new SuccessDataResult<List<Reservation>>(list);
        }

        // Owner rezervi təsdiq/rədd edir; təsdiq olunanda gün "Booked", rədd olunanda yenidən "Available" olur
        public async Task<IResult> UpdateStatusAsync(int reservationId, UpdateReservationStatusDto dto, int ownerId)
        {
            var reservation = await _reservationDal.GetAsync(r => r.Id == reservationId);
            if (reservation is null)
                return new ErrorResult("Rezerv tapılmadı.");

            var venue = await _venueDal.GetAsync(v => v.Id == reservation.VenueId && v.OwnerId == ownerId);
            if (venue is null)
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
    }
}
