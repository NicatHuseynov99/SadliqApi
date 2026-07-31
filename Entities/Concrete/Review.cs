using Core.Entities;

namespace Entities.Concrete
{
    // saray haqqında qonaq rəyi
    public class Review : IEntity, ISoftDelete
    {
        public int Id { get; set; }
        public int VenueId { get; set; }
        public Venue? Venue { get; set; }

        // Rəyi yazan qeydiyyatlı istifadəçidirsə saxlanılır (köçürülmüş rəylərdə boş ola bilər)
        public int? CustomerId { get; set; }
        public User? Customer { get; set; }

        public string AuthorName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Moderasiya: gizlədilmiş rəy saytda görünmür və reytinqə təsir etmir,
        // amma silinmir — lazım olsa geri qaytarıla bilər.
        public bool IsHidden { get; set; }
        public string? HiddenReason { get; set; }
        public DateTime? HiddenAt { get; set; }
        public int? HiddenByUserId { get; set; }

        // Şikayət sayı — moderatorun diqqətini yönəltmək üçün saxlanılır
        public int ReportCount { get; set; }

        // Silinmiş sətir saytda görünmür, amma bazada qalır — geri qaytarmaq olur
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
