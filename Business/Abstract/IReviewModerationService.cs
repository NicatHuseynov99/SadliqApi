using Core.Utilities.Results;
using Entities.Dtos.Common;
using Entities.Dtos.Review;

namespace Business.Abstract
{
    // Rəy moderasiyası — yalnız platforma admini və dəstək üçün
    public interface IReviewModerationService
    {
        Task<IDataResult<ReviewModerationStatsDto>> GetStatsAsync();
        Task<IDataResult<PagedResult<AdminReviewDto>>> SearchAsync(ReviewSearchDto dto);
        Task<IDataResult<List<ReviewReportDto>>> GetReportsAsync(int reviewId);

        // Rəyi gizlədir və ya geri açır; reytinq avtomatik yenidən hesablanır
        Task<IResult> SetHiddenAsync(int reviewId, SetReviewHiddenDto dto, int adminUserId);

        // Rəyi tamamilə silir — geri qaytarmaq mümkün deyil
        Task<IResult> DeleteAsync(int reviewId);

        // Şikayətləri baxılmış kimi bağlayır
        Task<IResult> ResolveReportsAsync(int reviewId);
    }
}
