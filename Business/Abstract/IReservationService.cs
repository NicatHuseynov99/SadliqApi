using Core.Utilities.Results;
using Entities.Concrete;
using Entities.Dtos.Reservation;

namespace Business.Abstract
{
    public interface IReservationService
    {
        Task<IDataResult<Reservation>> CreateAsync(CreateReservationDto dto, int customerId);
        Task<IDataResult<List<Reservation>>> GetByCustomerAsync(int customerId);
        Task<IDataResult<List<Reservation>>> GetByVenueAsync(int venueId, int ownerId);

        // Sahibin bütün saraylarına gələn rezervlər — panel tək sorğu ilə dolur
        Task<IDataResult<List<Reservation>>> GetByOwnerAsync(int ownerId);
        Task<IResult> UpdateStatusAsync(int reservationId, UpdateReservationStatusDto dto, int ownerId);
        Task<IResult> CancelAsync(int reservationId, int customerId);
    }
}
