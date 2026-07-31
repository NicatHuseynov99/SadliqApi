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
        private readonly IReviewEditDal _editDal;

        public ReviewModerationManager(
            IReviewDal reviewDal,
            IReviewReportDal reportDal,
            IVenueDal venueDal,
            IReviewEditDal editDal)
        {
            _reviewDal = reviewDal;
            _reportDal = reportDal;
            _venueDal = venueDal;
            _editDal = editDal;
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

        public async Task<IDataResult<PagedResult<AdminReviewDto>>> SearchAsync(ReviewSearchDto dto, UserRole viewerRole)
        {
            var query = (await _reviewDal.GetListAsync()).IgnoreQueryFilters();

            if (dto.VenueId.HasValue)
                query = query.Where(r => r.VenueId == dto.VenueId.Value);

            if (dto.Rating is >= 1 and <= 5)
                query = query.Where(r => r.Rating == dto.Rating.Value);

            query = dto.Status switch
            {
                "hidden" => query.Where(r => !r.IsDeleted && r.IsHidden),
                "visible" => query.Where(r => !r.IsDeleted && !r.IsHidden),
                "reported" => query.Where(r => !r.IsDeleted && r.ReportCount > 0),
                "deleted" => query.Where(r => r.IsDeleted),
                _ => query.Where(r => !r.IsDeleted)
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

            // Silinmiş sarayın rəyi də paneldə görünə bilər — filtri söndürürük
            var venues = await (await _venueDal.GetListAsync(v => venueIds.Contains(v.Id)))
                .IgnoreQueryFilters()
                .Select(v => new { v.Id, v.Name, v.City, v.CoverImageUrl })
                .ToListAsync();

            var reviewIds = rows.Select(r => r.Id).ToList();
            var openReports = await (await _reportDal.GetListAsync(r => reviewIds.Contains(r.ReviewId) && !r.IsResolved))
                .GroupBy(r => r.ReviewId)
                .Select(g => new { ReviewId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.ReviewId, g => g.Count);

            // Düzəliş tarixçəsi yalnız platforma sahibinə göndərilir.
            // Dəstək admini nə nişanı, nə də köhnə mətni görür.
            var edits = viewerRole == UserRole.SuperAdmin
                ? await (await _editDal.GetListAsync(e => reviewIds.Contains(e.ReviewId)))
                    .GroupBy(e => e.ReviewId)
                    .Select(g => new
                    {
                        ReviewId = g.Key,
                        Count = g.Count(),
                        LastAt = g.Max(e => e.CreatedAt)
                    })
                    .ToListAsync()
                : null;

            var lastEditors = viewerRole == UserRole.SuperAdmin
                ? await (await _editDal.GetListAsync(e => reviewIds.Contains(e.ReviewId)))
                    .Select(e => new { e.ReviewId, e.EditedByName, e.CreatedAt })
                    .ToListAsync()
                : null;

            var items = rows.Select(r =>
            {
                var venue = venues.FirstOrDefault(v => v.Id == r.VenueId);
                var edit = edits?.FirstOrDefault(e => e.ReviewId == r.Id);
                var lastEditor = lastEditors?
                    .Where(e => e.ReviewId == r.Id)
                    .OrderByDescending(e => e.CreatedAt)
                    .FirstOrDefault();

                return new AdminReviewDto
                {
                    Id = r.Id,
                    VenueId = r.VenueId,
                    VenueName = venue?.Name ?? "—",
                    VenueCity = venue?.City ?? string.Empty,
                    VenueImageUrl = venue?.CoverImageUrl,
                    CustomerId = r.CustomerId,
                    AuthorName = r.AuthorName,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    IsHidden = r.IsHidden,
                    HiddenReason = r.HiddenReason,
                    HiddenAt = r.HiddenAt,
                    ReportCount = r.ReportCount,
                    OpenReportCount = openReports.TryGetValue(r.Id, out var open) ? open : 0,
                    IsDeleted = r.IsDeleted,
                    DeletedAt = r.DeletedAt,
                    EditCount = edit?.Count ?? 0,
                    LastEditedBy = lastEditor?.EditedByName,
                    LastEditedAt = edit?.LastAt
                };
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

            // Silinmiş kimi işarələnir — «Silinmiş» süzgəcindən geri qaytarmaq olar
            await _reviewDal.DeleteAsync(review);
            return new SuccessResult("Rəy silindi (geri qaytarmaq olar).");
        }

        // Admin rəyin mətnini və balını düzəldir. Tam silmək əvəzinə bəzən sadəcə
        // şəxsi məlumatı və ya söyüşü çıxarmaq kifayət edir.
        public async Task<IResult> UpdateAsync(
            int reviewId, EditReviewDto dto, int adminUserId, string adminName, UserRole adminRole)
        {
            if (dto.Rating is < 1 or > 5)
                return new ErrorResult("Bal 1 ilə 5 arasında olmalıdır.");

            if (string.IsNullOrWhiteSpace(dto.Comment))
                return new ErrorResult("Rəy mətni boş ola bilməz.");

            var review = await FindAsync(reviewId);
            if (review is null)
                return new ErrorResult("Rəy tapılmadı.");

            var comment = dto.Comment.Trim();

            if (review.Rating == dto.Rating && review.Comment == comment)
                return new ErrorResult("Dəyişiklik yoxdur.");

            // Əvvəlki hal əvvəlcə yazılır — düzəliş uğursuz olsa belə tarixçə itməsin
            await _editDal.AddAsync(new ReviewEdit
            {
                ReviewId = review.Id,
                EditedByUserId = adminUserId,
                EditedByName = adminName,
                EditedByRole = adminRole.ToString(),
                OldRating = review.Rating,
                NewRating = dto.Rating,
                OldComment = review.Comment,
                NewComment = comment,
                CreatedAt = DateTime.UtcNow
            });

            review.Rating = dto.Rating;
            review.Comment = comment;
            await _reviewDal.UpdateAsync(review);

            return new SuccessResult("Rəy yeniləndi.");
        }

        // Düzəlişlərin əvvəlki və yeni halı. Çağırış icazəsi kontrollerdə SuperAdmin ilə
        // məhdudlaşdırılıb — dəstək admini öz izini silə və ya gizlədə bilmir.
        public async Task<IDataResult<List<ReviewEditDto>>> GetEditsAsync(int reviewId)
        {
            var query = await _editDal.GetListAsync(e => e.ReviewId == reviewId);

            var items = await query
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new ReviewEditDto
                {
                    Id = e.Id,
                    EditedByName = e.EditedByName,
                    EditedByRole = e.EditedByRole,
                    OldRating = e.OldRating,
                    NewRating = e.NewRating,
                    OldComment = e.OldComment,
                    NewComment = e.NewComment,
                    CreatedAt = e.CreatedAt
                })
                .ToListAsync();

            return new SuccessDataResult<List<ReviewEditDto>>(items);
        }

        public async Task<IResult> RestoreAsync(int reviewId)
        {
            var review = await FindAsync(reviewId);
            if (review is null)
                return new ErrorResult("Rəy tapılmadı.");

            if (!review.IsDeleted)
                return new ErrorResult("Bu rəy silinməyib.");

            review.IsDeleted = false;
            review.DeletedAt = null;
            await _reviewDal.UpdateAsync(review);

            return new SuccessResult("Rəy geri qaytarıldı.");
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
