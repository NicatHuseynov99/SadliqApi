using Core.Utilities.Results;
using Entities.Concrete;
using Entities.Dtos.Common;
using Entities.Dtos.Review;

namespace Business.Abstract
{
    // Rəy moderasiyası — yalnız platforma admini və dəstək üçün
    public interface IReviewModerationService
    {
        Task<IDataResult<ReviewModerationStatsDto>> GetStatsAsync();
        // Düzəliş nişanı yalnız platforma sahibinə göndərilir — ona görə rol lazımdır
        Task<IDataResult<PagedResult<AdminReviewDto>>> SearchAsync(ReviewSearchDto dto, UserRole viewerRole);
        Task<IDataResult<List<ReviewReportDto>>> GetReportsAsync(int reviewId);

        // Rəyin balını və mətnini düzəldir; əvvəlki hal tarixçəyə yazılır
        Task<IResult> UpdateAsync(int reviewId, EditReviewDto dto, int adminUserId, string adminName, UserRole adminRole);

        // Düzəliş tarixçəsi — yalnız SuperAdmin çağıra bilir
        Task<IDataResult<List<ReviewEditDto>>> GetEditsAsync(int reviewId);

        // Rəyi gizlədir və ya geri açır; reytinq avtomatik yenidən hesablanır
        Task<IResult> SetHiddenAsync(int reviewId, SetReviewHiddenDto dto, int adminUserId);

        // Rəyi silinmiş kimi işarələyir; admin onu görür və geri qaytara bilir
        Task<IResult> DeleteAsync(int reviewId);
        Task<IResult> RestoreAsync(int reviewId);

        // Şikayətləri baxılmış kimi bağlayır
        Task<IResult> ResolveReportsAsync(int reviewId);
    }
}
