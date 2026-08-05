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
        private readonly IReviewReportDal _reportDal;
        private readonly IPlatformSettingsService _settings;
        private readonly ITextCatalog _texts;
        private readonly IReservationDal _reservationDal;
        private readonly IVenueStaffDal _staffDal;

        public ReviewManager(
            IReviewDal reviewDal,
            IVenueDal venueDal,
            IReviewReportDal reportDal,
            IPlatformSettingsService settings,
            ITextCatalog texts,
            IReservationDal reservationDal,
            IVenueStaffDal staffDal)
        {
            _reservationDal = reservationDal;
            _staffDal = staffDal;
            _reviewDal = reviewDal;
            _venueDal = venueDal;
            _reportDal = reportDal;
            _settings = settings;
            _texts = texts;
        }

        // Sarayın bütün rəyləri — yenilər yuxarıda
        public async Task<IDataResult<List<Review>>> GetByVenueAsync(int venueId)
        {
            var query = await _reviewDal.GetListAsync(r => r.VenueId == venueId);
            var reviews = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();

            return new SuccessDataResult<List<Review>>(reviews);
        }

        // Yeni rəy. Rəy yazmaq üçün həmin sarayda keçmiş, təsdiqlənmiş tədbir
        // olmalıdır — yoxsa reytinq heç nə ifadə etmir: ora getməmiş adam da,
        // sarayın öz işçisi də beş ulduz yaza bilərdi.
        public async Task<IDataResult<Review>> CreateAsync(CreateReviewDto dto, int userId, string authorName)
        {
            var validation = Validate(dto.Rating, dto.Comment);
            if (validation is not null)
                return new ErrorDataResult<Review>(validation);

            var venue = await _venueDal.GetAsync(v => v.Id == dto.VenueId);
            if (venue is null)
                return new ErrorDataResult<Review>("Saray tapılmadı.");

            // Öz sarayına rəy yazmaq olmaz
            if (venue.OwnerId == userId)
                return new ErrorDataResult<Review>("Öz sarayınıza rəy yaza bilməzsiniz.");

            if (await _staffDal.GetAsync(s => s.VenueId == dto.VenueId && s.UserId == userId && s.IsActive) is not null)
                return new ErrorDataResult<Review>("İşlədiyiniz saraya rəy yaza bilməzsiniz.");

            // Keçmiş, təsdiqlənmiş tədbir tapılır
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

            var visitsQuery = await _reservationDal.GetListAsync(
                r => r.CustomerId == userId
                     && r.VenueId == dto.VenueId
                     && r.Status == ReservationStatus.Confirmed
                     && r.EventDate < today);

            var visits = await visitsQuery.OrderByDescending(r => r.EventDate).ToListAsync();

            if (visits.Count == 0)
                return new ErrorDataResult<Review>(
                    "Rəy yalnız tədbiri keçmiş qonaqlardan qəbul olunur. " +
                    "Bu sarayda təsdiqlənmiş və tarixi keçmiş rezerviniz görünmür.");

            // Hər tədbirə bir rəy
            var usedQuery = await _reviewDal.GetListAsync(
                r => r.CustomerId == userId && r.VenueId == dto.VenueId && r.ReservationId != null);

            var usedIds = await usedQuery.Select(r => r.ReservationId!.Value).ToListAsync();

            var visit = visits.FirstOrDefault(v => !usedIds.Contains(v.Id));

            if (visit is null)
                return new ErrorDataResult<Review>(
                    "Bu saraydakı tədbiriniz üçün artıq rəy yazmısınız. Mövcud rəyinizi redaktə edə bilərsiniz.");

            var review = new Review
            {
                VenueId = dto.VenueId,
                CustomerId = userId,
                AuthorName = authorName,
                Rating = dto.Rating,
                Comment = dto.Comment,
                CreatedAt = DateTime.UtcNow,
                IsVerifiedGuest = true,
                ReservationId = visit.Id
            };

            await _reviewDal.AddAsync(review);

            // Cavabda bütün saray obyekti qayıtmasın deyə naviqasiya sahəsi təmizlənir
            review.Venue = null;
            return new SuccessDataResult<Review>(review, "Rəyiniz əlavə olundu.");
        }

        // Səhifə rəy formasını göstərməzdən əvvəl soruşur: bu adam yaza bilərmi?
        // Eyni qaydalar CreateAsync-də yenidən yoxlanılır — bu yalnız interfeys üçündür.
        public async Task<IDataResult<ReviewEligibilityDto>> CanReviewAsync(int venueId, int userId)
        {
            var venue = await _venueDal.GetAsync(v => v.Id == venueId);

            if (venue is null)
                return new SuccessDataResult<ReviewEligibilityDto>(
                    new ReviewEligibilityDto { CanReview = false, Reason = "Saray tapılmadı." });

            if (venue.OwnerId == userId)
                return new SuccessDataResult<ReviewEligibilityDto>(
                    new ReviewEligibilityDto { CanReview = false, Reason = "Öz sarayınıza rəy yaza bilməzsiniz." });

            if (await _staffDal.GetAsync(s => s.VenueId == venueId && s.UserId == userId && s.IsActive) is not null)
                return new SuccessDataResult<ReviewEligibilityDto>(
                    new ReviewEligibilityDto { CanReview = false, Reason = "İşlədiyiniz saraya rəy yaza bilməzsiniz." });

            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

            var visitsQuery = await _reservationDal.GetListAsync(
                r => r.CustomerId == userId
                     && r.VenueId == venueId
                     && r.Status == ReservationStatus.Confirmed
                     && r.EventDate < today);

            var visitIds = await visitsQuery.Select(r => r.Id).ToListAsync();

            if (visitIds.Count == 0)
                return new SuccessDataResult<ReviewEligibilityDto>(new ReviewEligibilityDto
                {
                    CanReview = false,
                    Reason = "Rəy yalnız tədbiri keçmiş qonaqlardan qəbul olunur."
                });

            var usedQuery = await _reviewDal.GetListAsync(
                r => r.CustomerId == userId && r.VenueId == venueId && r.ReservationId != null);

            var usedIds = await usedQuery.Select(r => r.ReservationId!.Value).ToListAsync();

            if (visitIds.All(usedIds.Contains))
                return new SuccessDataResult<ReviewEligibilityDto>(new ReviewEligibilityDto
                {
                    CanReview = false,
                    Reason = "Tədbiriniz üçün rəy yazmısınız — onu redaktə edə bilərsiniz."
                });

            return new SuccessDataResult<ReviewEligibilityDto>(new ReviewEligibilityDto { CanReview = true });
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

        // İstifadəçi rəyi şikayət edir. Rəy dərhal gizlənmir — qərarı moderator verir.
        public async Task<IResult> ReportAsync(ReportReviewDto dto, int userId)
        {
            // İcazəli səbəblər bazadakı kitabçadan gəlir
            if (!_texts.Map(TextCategories.ReportReason).ContainsKey(dto.Reason ?? string.Empty))
                return new ErrorResult("Şikayət səbəbi düzgün seçilməyib.");

            var review = await _reviewDal.GetAsync(r => r.Id == dto.ReviewId);
            if (review is null)
                return new ErrorResult("Rəy tapılmadı.");

            if (review.CustomerId == userId)
                return new ErrorResult("Öz rəyinizi şikayət edə bilməzsiniz.");

            var existing = await _reportDal.GetAsync(r => r.ReviewId == dto.ReviewId && r.ReporterUserId == userId);
            if (existing is not null)
                return new ErrorResult("Bu rəyi artıq şikayət etmisiniz.");

            await _reportDal.AddAsync(new ReviewReport
            {
                ReviewId = dto.ReviewId,
                ReporterUserId = userId,
                Reason = dto.Reason,
                Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim(),
                CreatedAt = DateTime.UtcNow
            });

            // Sayğac moderasiya siyahısında sıralama üçün saxlanılır
            review.ReportCount += 1;

            // Həddi keçəndə rəy moderator baxana qədər avtomatik gizlədilir.
            // Hədd 0 olanda bu davranış söndürülüb.
            var threshold = await _settings.GetIntAsync("review_auto_hide_reports", 0);
            var autoHidden = threshold > 0 && !review.IsHidden && review.ReportCount >= threshold;

            if (autoHidden)
            {
                review.IsHidden = true;
                review.HiddenReason = $"Avtomatik: {review.ReportCount} şikayət";
                review.HiddenAt = DateTime.UtcNow;
            }

            await _reviewDal.UpdateAsync(review);

            return new SuccessResult(autoHidden
                ? "Şikayətiniz qeydə alındı, rəy yoxlanana qədər gizlədildi."
                : "Şikayətiniz moderatora göndərildi.");
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
