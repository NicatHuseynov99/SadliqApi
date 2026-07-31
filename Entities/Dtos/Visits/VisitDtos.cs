namespace Entities.Dtos.Visits
{
    // Frontdan gələn səhifə baxışı. IP və brauzer serverdə doldurulur —
    // müştəri tərəfə etibar edilmir.
    public class TrackPageDto
    {
        public string SessionId { get; set; } = string.Empty;
        public string PageType { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public int? VenueId { get; set; }
        public string? Title { get; set; }
        public string? Referrer { get; set; }
    }

    // Admin paneldəki ziyarətçi siyahısının süzgəcləri
    public class VisitSearchDto
    {
        public string? Q { get; set; }
        public string? PageType { get; set; }

        // "guest" — yalnız qeydiyyatsızlar, "user" — yalnız girmiş istifadəçilər
        public string? Audience { get; set; }

        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 30;
    }

    public class VisitRowDto
    {
        public int Id { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public string? UserFullName { get; set; }
        public string PageType { get; set; } = string.Empty;
        public string PageLabel { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public int? VenueId { get; set; }
        public string? Title { get; set; }
        public string? Referrer { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class VisitStatsDto
    {
        public int TotalViews { get; set; }
        public int UniqueVisitors { get; set; }
        public int GuestVisitors { get; set; }
        public int TodayViews { get; set; }
        public int TodayVisitors { get; set; }

        // Son 14 günün gündəlik mənzərəsi — qrafik üçün
        public List<DailyVisitDto> Daily { get; set; } = new();

        public List<TopVenueDto> TopVenues { get; set; } = new();
        public List<TopPageDto> TopPages { get; set; } = new();
    }

    public class DailyVisitDto
    {
        public DateTime Date { get; set; }
        public int Views { get; set; }
        public int Visitors { get; set; }
    }

    public class TopVenueDto
    {
        public int VenueId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Views { get; set; }
        public int Visitors { get; set; }
    }

    public class TopPageDto
    {
        public string PageType { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public int Views { get; set; }
    }
}
