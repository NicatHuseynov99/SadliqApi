using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos.Review;
using Microsoft.EntityFrameworkCore;

namespace Business.Concrete
{
    public class ReviewManager : IReviewService
    {
        private readonly IReviewDal _reviewDal;
        private readonly IVenueDal _venueDal;

        public ReviewManager(IReviewDal reviewDal, IVenueDal venueDal)
        {
            _reviewDal = reviewDal;
            _venueDal = venueDal;
        }

        // Sarayın bütün rəyləri — yenilər yuxarıda
        public async Task<IDataResult<List<Review>>> GetByVenueAsync(int venueId)
        {
            var query = await _reviewDal.GetListAsync(r => r.VenueId == venueId);
            var reviews = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();

            return new SuccessDataResult<List<Review>>(reviews);
        }

        // Yeni rəy əlavə edir — bir istifadəçi eyni saraya bir neçə rəy yaza bilər
        public async Task<IDataResult<Review>> CreateAsync(CreateReviewDto dto, int userId, string authorName)
        {
            var validation = Validate(dto.Rating, dto.Comment);
            if (validation is not null)
                return new ErrorDataResult<Review>(validation);

            var venue = await _venueDal.GetAsync(v => v.Id == dto.VenueId);
            if (venue is null)
                return new ErrorDataResult<Review>("Saray tapılmadı.");

            var review = new Review
            {
                VenueId = dto.VenueId,
                CustomerId = userId,
                AuthorName = authorName,
                Rating = dto.Rating,
                Comment = dto.Comment,
                CreatedAt = DateTime.UtcNow
            };

            await _reviewDal.AddAsync(review);

            // Cavabda bütün saray obyekti qayıtmasın deyə naviqasiya sahəsi təmizlənir
            review.Venue = null;
            return new SuccessDataResult<Review>(review, "Rəyiniz əlavə olundu.");
        }

        // İstifadəçi yalnız öz rəyini redaktə edə bilər
        public async Task<IDataResult<Review>> UpdateAsync(int reviewId, UpdateReviewDto dto, int userId)
        {
            var validation = Validate(dto.Rating, dto.Comment);
            if (validation is not null)
                return new ErrorDataResult<Review>(validation);

            var review = await _reviewDal.GetAsync(r => r.Id == reviewId);
            if (review is null)
                return new ErrorDataResult<Review>("Rəy tapılmadı.");

            if (review.CustomerId != userId)
                return new ErrorDataResult<Review>("Bu rəy sizə aid deyil.");

            review.Rating = dto.Rating;
            review.Comment = dto.Comment;
            await _reviewDal.UpdateAsync(review);

            review.Venue = null;
            return new SuccessDataResult<Review>(review, "Rəy yeniləndi.");
        }

        // İstifadəçi yalnız öz rəyini silə bilər
        public async Task<IResult> DeleteAsync(int reviewId, int userId)
        {
            var review = await _reviewDal.GetAsync(r => r.Id == reviewId);
            if (review is null)
                return new ErrorResult("Rəy tapılmadı.");

            if (review.CustomerId != userId)
                return new ErrorResult("Bu rəy sizə aid deyil.");

            await _reviewDal.DeleteAsync(review);
            return new SuccessResult("Rəy silindi.");
        }

        // Bal və mətn yoxlaması — xəta varsa mesaj, yoxdursa null qaytarır
        private static string? Validate(int rating, string comment)
        {
            if (rating is < 1 or > 5)
                return "Bal 1 ilə 5 arasında olmalıdır.";

            if (string.IsNullOrWhiteSpace(comment))
                return "Rəy mətni boş ola bilməz.";

            return null;
        }
    }
}
