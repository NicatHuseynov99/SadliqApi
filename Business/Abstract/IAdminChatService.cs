using Core.Utilities.Results;
using Entities.Dtos.Admin;
using Entities.Dtos.Common;

namespace Business.Abstract
{
    // Platforma admininin yazışmalara nəzarəti. Admin yazışmaya yaza bilmir və
    // mesajı silə bilmir — yazışma mübahisədə dəlildir. Yeganə müdaxilə:
    // qanunsuz mesajı tərəflərdən gizlətmək (mesaj bazada qalır, admin görür).
    public interface IAdminChatService
    {
        Task<IDataResult<PagedResult<AdminChatSummaryDto>>> SearchAsync(AdminChatQueryDto query);

        Task<IDataResult<AdminChatDetailDto>> GetAsync(int offerId);

        // Birbaşa rezervin yazışması — təklifsiz axın
        Task<IDataResult<AdminChatDetailDto>> GetReservationAsync(int reservationId);

        Task<IResult> SetHiddenAsync(int messageId, SetMessageHiddenDto dto, int adminUserId);
    }
}
