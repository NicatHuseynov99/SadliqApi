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
        Task<IResult> UpdateStatusAsync(int reservationId, UpdateReservationStatusDto dto, int ownerId);
    }
}
