using Core.Utilities.Results;
using Entities.Dtos.VenueRequest;

namespace Business.Abstract
{
    // Tərs axtarış: müştəri şərtlərini yazır, saraylar təklif göndərir.
    public interface IVenueRequestService
    {
        // ————— müştəri —————
        Task<IDataResult<VenueRequestDto>> CreateAsync(CreateVenueRequestDto dto, int customerId);
        Task<IDataResult<List<VenueRequestDto>>> GetMineAsync(int customerId);
        Task<IResult> CloseAsync(int requestId, int customerId);

        // Təklifi qəbul etmək rezerv yaradır və sorğunu bağlayır
        Task<IResult> AcceptOfferAsync(int offerId, int customerId);
        Task<IResult> DeclineOfferAsync(int offerId, int customerId);

        // ————— saray —————
        Task<IDataResult<List<OpenRequestDto>>> GetOpenForOwnerAsync(int ownerId);
        Task<IResult> CreateOfferAsync(CreateVenueOfferDto dto, int ownerId);
        Task<IResult> WithdrawOfferAsync(int offerId, int ownerId);

        // Sahibin göndərdiyi təkliflər — yazışmalar buradan açılır
        Task<IDataResult<List<MyOfferDto>>> GetMyOffersAsync(int ownerId);

        // ————— yazışma —————
        // Müştəri təklifi təsdiqləyir: rezerv hələ yaranmır, sadəcə söhbət açılır
        Task<IResult> ApproveContactAsync(int offerId, int customerId);

        Task<IDataResult<List<OfferMessageDto>>> GetMessagesAsync(int offerId, int userId);
        Task<IDataResult<OfferMessageDto>> SendMessageAsync(SendMessageDto dto, int userId);

        // Arxa fon: müddəti bitmiş sorğuları bağlayır
        Task<int> ExpireOldAsync(CancellationToken token = default);
    }
}
