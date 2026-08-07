using Core.Utilities.Results;
using Entities.Concrete;
using Entities.Dtos.Review;

namespace Business.Abstract
{
    public interface IReviewService
    {
        Task<IDataResult<List<Review>>> GetByVenueAsync(int venueId);

        // Qeydiyyatdan keçmiş istifadəçi rəy yazır — sayına məhdudiyyət yoxdur
        Task<IDataResult<Review>> CreateAsync(CreateReviewDto dto, int userId, string authorName);

        Task<IDataResult<Review>> UpdateAsync(int reviewId, UpdateReviewDto dto, int userId);
        Task<IResult> DeleteAsync(int reviewId, int userId);

        // İstifadəçi uyğunsuz rəyi moderatorun diqqətinə çatdırır
        Task<IResult> ReportAsync(ReportReviewDto dto, int userId);

        // Rəy yaza bilərmi: səhifə formanı boş yerə göstərməsin
        Task<IDataResult<ReviewEligibilityDto>> CanReviewAsync(int venueId, int userId);

        // Sarayın rəyə açıq cavabı — bir rəyə bir cavab, redaktə edilə bilir
        Task<IResult> ReplyAsync(int reviewId, ReplyReviewDto dto, int userId);
        Task<IResult> DeleteReplyAsync(int reviewId, int userId);
    }
}
