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

        // Platforma admini üçün — sahiblik yoxlaması aparılmır
        Task<IResult> AdminUpdateStatusAsync(int reservationId, UpdateReservationStatusDto dto, int adminUserId);
        Task<IResult> CancelAsync(int reservationId, int customerId, string? reason = null);

        // Sahib sorğunu açdı — müştəri «baxıldı» görsün
        Task<IResult> MarkSeenAsync(int reservationId, int ownerId);

        // «Rədd et» əvəzinə başqa tarix / zal / menyu / qiymət təklif etmək
        Task<IDataResult<ReservationCounterOffer>> CreateCounterOfferAsync(CreateCounterOfferDto dto, int ownerId);

        // Müştərinin cavabı
        Task<IResult> AcceptCounterOfferAsync(int offerId, int customerId);
        Task<IResult> DeclineCounterOfferAsync(int offerId, int customerId);

        // Danışıq iki tərəflidir: müştəri də öz şərtini təklif edə bilər
        Task<IDataResult<ReservationCounterOffer>> CreateCustomerCounterOfferAsync(
            CreateCounterOfferDto dto, int customerId);

        Task<IResult> AcceptCustomerOfferAsync(int offerId, int ownerId);
        Task<IResult> DeclineCustomerOfferAsync(int offerId, int ownerId);
    }
}
