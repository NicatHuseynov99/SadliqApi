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
    }
}
