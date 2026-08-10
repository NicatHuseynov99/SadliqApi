using Core.Utilities.Results;
using Entities.Dtos.Common;
using Entities.Dtos.Dispute;

namespace Business.Abstract
{
    // Şikayət/mübahisə işləri. Tərəflər iş açır, dəstək işi aparır və qərar verir.
    // Qərarın izi qalır — əvvəlki birtərəfli admin müdaxiləsindən fərqi budur.
    public interface IDisputeService
    {
        // ————— tərəflər —————

        Task<IDataResult<int>> CreateAsync(CreateDisputeDto dto, int userId);

        // İstifadəçinin öz işləri (açdığı və ya tərəfi olduğu)
        Task<IDataResult<List<DisputeSummaryDto>>> GetMineAsync(int userId);

        // ————— hər iki tərəf —————

        // Tərəf öz işini, dəstək isə istənilən işi görür
        Task<IDataResult<DisputeDetailDto>> GetAsync(int disputeId, int userId);

        Task<IResult> SendMessageAsync(int disputeId, SendDisputeMessageDto dto, int userId);

        // ————— dəstək —————

        Task<IDataResult<PagedResult<DisputeSummaryDto>>> SearchAsync(DisputeQueryDto query);

        // İşi öz üzərinə götürmək
        Task<IResult> AssignAsync(int disputeId, int staffUserId);

        Task<IResult> ResolveAsync(int disputeId, ResolveDisputeDto dto, int staffUserId);
    }
}
