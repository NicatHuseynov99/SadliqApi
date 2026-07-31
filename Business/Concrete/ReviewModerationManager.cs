using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos.Common;
using Entities.Dtos.Review;
using Microsoft.EntityFrameworkCore;

namespace Business.Concrete
{
    // Rəy moderasiyası. Gizlədilmiş rəylər üzərində işlədiyimiz üçün bütün
    // sorğularda IgnoreQueryFilters() çağırılır — panel hər şeyi görməlidir.
    public class ReviewModerationManager : IReviewModerationService
    {
        private readonly IReviewDal _reviewDal;
        private readonly IReviewReportDal _reportDal;
        private readonly IVenueDal _venueDal;

        public ReviewModerationManager(IReviewDal reviewDal, IReviewReportDal reportDal, IVenueDal venueDal)
        {
            _reviewDal = reviewDal;
            _reportDal = reportDal;
            _venueDal = venueDal;
        }

        public async Task<IDataResult<ReviewModerationStatsDto>> GetStatsAsync()
        {
            var all = (await _reviewDal.GetListAsync()).IgnoreQueryFilters();
            var reports = await _reportDal.GetListAsync();

            var stats = new ReviewModerationStatsDto
            {
                TotalReviews = await all.CountAsync(),
                HiddenReviews = await all.CountAsync(r => r.IsHidden),
                ReportedReviews = await all.CountAsync(r => r.ReportCount > 0),
                OpenReports = await reports.CountAsync(r => !r.IsResolved),

                // Ortalama yalnız görünən rəylər üzrə — saytda göstərilən rəqəmlə eyni olsun
                AverageRating = Math.Round(
                    await all.Where(r => !r.IsHidden).Select(r => (double?)r.Rating).AverageAsync() ?? 0, 2)
            };

            return new SuccessDataResult<ReviewModerationStatsDto>(stats);
        }

        public async Task<IDataResult<PagedResult<AdminReviewDto>>> SearchAsync(ReviewSearchDto dto)
        {
            var query = (await _reviewDal.GetListAsync()).IgnoreQueryFilters();

            if (dto.VenueId.HasValue)
                query = query.Where(r => r.VenueId == dto.VenueId.Value);

            if (dto.Rating is >= 1 and <= 5)
                query = query.Where(r => r.Rating == dto.Rating.Value);

            query = dto.Status switch
            {
                "hidden" => query.Where(r => r.IsHidden),
                "visible" => query.Where(r => !r.IsHidden),
                "reported" => query.Where(r => r.ReportCount > 0),
                _ => query
            };

            if (!string.IsNullOrWhiteSpace(dto.Q))
            {
                var q = dto.Q.Trim().ToLower();
                query = query.Where(r =>
                    r.Comment.ToLower().Contains(q) ||
                    r.AuthorName.ToLower().Contains(q));
            }

            var total = await query.CountAsync();

            var page = dto.Page < 1 ? 1 : dto.Page;
            var pageSize = dto.PageSize is < 1 or > 100 ? 20 : dto.PageSize;

            // Şikayət olunanlar öndə, sonra yenilər — moderator ən vacibini əvvəl görsün
            var rows = await query
                .OrderByDescending(r => r.ReportCount)
                .ThenByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var venueIds = rows.Select(r => r.VenueId).Distinct().ToList();
            var venueNames = await (await _venueDal.GetListAsync(v => venueIds.Contains(v.Id)))
                .Select(v => new { v.Id, v.Name })
                .ToDictionaryAsync(v => v.Id, v => v.Name);

            var reviewIds = rows.Select(r => r.Id).ToList();
            var openReports = await (await _reportDal.GetListAsync(r => reviewIds.Contains(r.ReviewId) && !r.IsResolved))
                .GroupBy(r => r.ReviewId)
                .Select(g => new { ReviewId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.ReviewId, g => g.Count);

            var items = rows.Select(r => new AdminReviewDto
            {
                Id = r.Id,
                VenueId = r.VenueId,
                VenueName = venueNames.TryGetValue(r.VenueId, out var name) ? name : "—",
                CustomerId = r.CustomerId,
                AuthorName = r.AuthorName,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                IsHidden = r.IsHidden,
                HiddenReason = r.HiddenReason,
                HiddenAt = r.HiddenAt,
                ReportCount = r.ReportCount,
                OpenReportCount = openReports.TryGetValue(r.Id, out var open) ? open : 0
            }).ToList();

            var result = new PagedResult<AdminReviewDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };

            return new SuccessDataResult<PagedResult<AdminReviewDto>>(result);
        }

        public async Task<IDataResult<List<ReviewReportDto>>> GetReportsAsync(int reviewId)
        {
            var query = await _reportDal.GetListAsync(r => r.ReviewId == reviewId);

            var reports = await query
                .Include(r => r.ReporterUser)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var items = reports.Select(r => new ReviewReportDto
            {
                Id = r.Id,
                Reason = r.Reason,
                ReasonLabel = ReasonLabel(r.Reason),
                Note = r.Note,
                ReporterName = r.ReporterUser?.FullName,
                IsResolved = r.IsResolved,
                CreatedAt = r.CreatedAt
            }).ToList();

            return new SuccessDataResult<List<ReviewReportDto>>(items);
        }

        public async Task<IResult> SetHiddenAsync(int reviewId, SetReviewHiddenDto dto, int adminUserId)
        {
            var review = await FindAsync(reviewId);
            if (review is null)
                return new ErrorResult("Rəy tapılmadı.");

            if (review.IsHidden == dto.IsHidden)
                return new ErrorResult(dto.IsHidden ? "Rəy artıq gizlədilib." : "Rəy artıq görünür.");

            if (dto.IsHidden && string.IsNullOrWhiteSpace(dto.Reason))
                return new ErrorResult("Gizlətmə səbəbini yazın.");

            review.IsHidden = dto.IsHidden;
            review.HiddenReason = dto.IsHidden ? dto.Reason!.Trim() : null;
            review.HiddenAt = dto.IsHidden ? DateTime.UtcNow : null;
            review.HiddenByUserId = dto.IsHidden ? adminUserId : null;

            await _reviewDal.UpdateAsync(review);

            // Gizlədiləndə şikayətlər həll olunmuş sayılır — məsələ bağlandı
            if (dto.IsHidden)
                await CloseReportsAsync(reviewId);

            return new SuccessResult(dto.IsHidden
                ? "Rəy gizlədildi və reytinqdən çıxarıldı."
                : "Rəy yenidən göstərilir.");
        }

        public async Task<IResult> DeleteAsync(int reviewId)
        {
            var review = await FindAsync(reviewId);
            if (review is null)
                return new ErrorResult("Rəy tapılmadı.");

            // Şikayətlər kaskadla silinir (DbContext-də təyin olunub)
            await _reviewDal.DeleteAsync(review);
            return new SuccessResult("Rəy tamamilə silindi.");
        }

        public async Task<IResult> ResolveReportsAsync(int reviewId)
        {
            var closed = await CloseReportsAsync(reviewId);
            if (closed == 0)
                return new ErrorResult("Bu rəydə açıq şikayət yoxdur.");

            return new SuccessResult($"{closed} şikayət bağlandı.");
        }

        // Gizlədilmiş rəyi də tapa bilmək üçün filtri söndürürük
        private async Task<Review?> FindAsync(int reviewId)
        {
            var query = (await _reviewDal.GetListAsync()).IgnoreQueryFilters();
            return await query.FirstOrDefaultAsync(r => r.Id == reviewId);
        }

        private async Task<int> CloseReportsAsync(int reviewId)
        {
            var query = await _reportDal.GetListAsync(r => r.ReviewId == reviewId && !r.IsResolved);
            var open = await query.ToListAsync();

            foreach (var report in open)
            {
                report.IsResolved = true;
                report.ResolvedAt = DateTime.UtcNow;
                await _reportDal.UpdateAsync(report);
            }

            return open.Count;
        }

        // Şikayət səbəbinin oxunaqlı adı
        private static string ReasonLabel(string reason) => reason switch
        {
            "spam" => "Reklam / spam",
            "offensive" => "Təhqiramiz dil",
            "fake" => "Saxta rəy",
            "irrelevant" => "Mövzuya aid deyil",
            _ => "Digər"
        };
    }
}
