namespace Entities.Dtos.Review
{
    // İstifadəçinin şikayəti
    public class ReportReviewDto
    {
        public int ReviewId { get; set; }

        // spam | offensive | fake | irrelevant | other
        public string Reason { get; set; } = string.Empty;
        public string? Note { get; set; }
    }

    // Moderasiya siyahısının süzgəcləri
    public class ReviewSearchDto
    {
        // Mətn, müəllif adı və ya saray adı üzrə axtarış
        public string? Q { get; set; }
        public int? VenueId { get; set; }

        // all | visible | hidden | reported | deleted
        public string? Status { get; set; }
        public int? Rating { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class AdminReviewDto
    {
        public int Id { get; set; }
        public int VenueId { get; set; }
        public string VenueName { get; set; } = string.Empty;

        // Rəyin hansı saraya yazıldığı bir baxışda görünsün
        public string? VenueImageUrl { get; set; }
        public string VenueCity { get; set; } = string.Empty;
        public int? CustomerId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public bool IsHidden { get; set; }
        public string? HiddenReason { get; set; }
        public DateTime? HiddenAt { get; set; }
        public int ReportCount { get; set; }
        public int OpenReportCount { get; set; }

        // Silinmiş rəy yalnız adminə görünür və geri qaytarıla bilir
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Rəy admin tərəfindən neçə dəfə düzəldilib. Bu sahə YALNIZ platforma
        // sahibinə doldurulur — dəstək admini öz düzəlişini görmür.
        public int EditCount { get; set; }
        public string? LastEditedBy { get; set; }
        public DateTime? LastEditedAt { get; set; }
    }

    // Bir düzəlişin əvvəlki və yeni halı
    public class ReviewEditDto
    {
        public int Id { get; set; }
        public string EditedByName { get; set; } = string.Empty;
        public string EditedByRole { get; set; } = string.Empty;
        public int OldRating { get; set; }
        public int NewRating { get; set; }
        public string OldComment { get; set; } = string.Empty;
        public string NewComment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    // Bir rəyin şikayətləri
    public class ReviewReportDto
    {
        public int Id { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string ReasonLabel { get; set; } = string.Empty;
        public string? Note { get; set; }
        public string? ReporterName { get; set; }
        public bool IsResolved { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Admin rəyin balını və mətnini düzəldir (məsələn şəxsi məlumat silinir)
    public class EditReviewDto
    {
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
    }

    public class SetReviewHiddenDto
    {
        public bool IsHidden { get; set; }

        // Gizlədəndə səbəb yazılır, açanda boş qala bilər
        public string? Reason { get; set; }
    }

    // Moderasiya bölməsinin başlıq göstəriciləri
    public class ReviewModerationStatsDto
    {
        public int TotalReviews { get; set; }
        public int HiddenReviews { get; set; }
        public int ReportedReviews { get; set; }
        public int OpenReports { get; set; }
        public double AverageRating { get; set; }
    }
}
