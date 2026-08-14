using Core.Utilities.Results;
using Entities.Dtos.VenueRequest;

namespace Business.Abstract
{
    // Birbaşa rezervin yazışması.
    //
    // Təklif üzrə söhbət VenueRequestManager-də yaşayır və ona toxunulmur.
    // Müştəri sarayın səhifəsindən birbaşa rezerv edəndə isə təklif yaranmır,
    // deməli orada yazışma da açılmırdı — bu servis həmin boşluğu doldurur.
    // Mesajlar eyni OfferMessage cədvəlində saxlanılır ki, adminin yazışma
    // görünüşü hər iki növü bir yerdə görsün.
    public interface IReservationChatService
    {
        Task<IDataResult<List<OfferMessageDto>>> GetMessagesAsync(int reservationId, int userId);

        Task<IDataResult<OfferMessageDto>> SendMessageAsync(
            int reservationId, string body, int userId);
    }
}
