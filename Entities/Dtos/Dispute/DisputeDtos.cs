using Entities.Concrete;

namespace Entities.Dtos.Dispute
{
    // Müştəri və ya saray yeni şikayət açır
    public class CreateDisputeDto
    {
        public DisputeCategory Category { get; set; } = DisputeCategory.Other;

        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Rezervlə bağlıdırsa — saray ondan avtomatik çıxarılır
        public int? ReservationId { get; set; }

        // Rezerv göstərilməyibsə birbaşa saray göstərilə bilər
        public int? VenueId { get; set; }
    }

    // Dəstək növbəsinin süzgəcləri
    public class DisputeQueryDto
    {
        // Open | InReview | Resolved | Closed
        public string? Status { get; set; }
        public DisputeCategory? Category { get; set; }

        // Mövzu, izah və ya tərəflərin adı üzrə
        public string? Search { get; set; }

        // Yalnız mənə təyin olunanlar
        public bool? MineOnly { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class DisputeSummaryDto
    {
        public int Id { get; set; }

        public string Subject { get; set; } = string.Empty;
        public DisputeCategory Category { get; set; }
        public string CategoryLabel { get; set; } = string.Empty;

        public DisputeStatus Status { get; set; }
        public string StatusLabel { get; set; } = string.Empty;

        public string OpenedByName { get; set; } = string.Empty;
        public bool OpenedByCustomer { get; set; }

        public string? VenueName { get; set; }
        public int? ReservationId { get; set; }

        public string? AssignedToName { get; set; }

        public int MessageCount { get; set; }
        public DateTime? LastMessageAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    public class DisputeDetailDto : DisputeSummaryDto
    {
        public string Description { get; set; } = string.Empty;
        public string? Resolution { get; set; }

        // Qarşı tərəfin adı — dəstək kimin kimdən şikayət etdiyini görməlidir
        public string? CounterpartName { get; set; }

        public List<DisputeMessageDto> Messages { get; set; } = new();
    }

    public class DisputeMessageDto
    {
        public int Id { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public bool IsFromStaff { get; set; }
        public bool IsInternalNote { get; set; }
        public string Body { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class SendDisputeMessageDto
    {
        public string Body { get; set; } = string.Empty;

        // Yalnız dəstək üçün: tərəflərə görünməyən qeyd
        public bool IsInternalNote { get; set; }
    }

    // Dəstək işi bağlayır
    public class ResolveDisputeDto
    {
        // Resolved və ya Closed
        public DisputeStatus Status { get; set; } = DisputeStatus.Resolved;

        // Qərarın izahı — məcburidir, tərəflər bunu görür
        public string Resolution { get; set; } = string.Empty;
    }
}
