using Core.Entities;

namespace Entities.Concrete
{
    // sistemdə görülən hər əməliyyatın izi — redaktə və silmə endpoint-i yoxdur
    public class AuditLog : IEntity
    {
        public int Id { get; set; }

        // Əməliyyatı edən — hesab sonradan silinsə də ad və rol surət kimi qalır
        public int? UserId { get; set; }
        public string UserFullName { get; set; } = "Qonaq";
        public UserRole? UserRole { get; set; }

        // Əməliyyatın kodu (VenueCreated, ReservationConfirmed...) və Azərbaycanca izahı
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Hansı obyekt üzərində: Venue, Hall, Menu, Reservation, Review, User...
        public string? EntityType { get; set; }
        public int? EntityId { get; set; }

        // Saraya aid əməliyyatlarda doldurulur ki, sonradan sahibə öz jurnalı göstərilə bilsin
        public int? VenueId { get; set; }

        public string HttpMethod { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public long DurationMs { get; set; }

        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

        public bool IsSuccess { get; set; } = true;
        public string? ErrorMessage { get; set; }

        // Sorğunun məzmunu (parol və token kimi sahələr gizlədilmiş halda)
        public string? Payload { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
