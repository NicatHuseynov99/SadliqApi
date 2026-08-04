using Core.Entities;

namespace Entities.Concrete
{
    // Admin (və ya saray sahibi) tərəfindən göndərilən elan.
    // Auditoriya şərtləri sətirdə saxlanılır ki, göndərişdən sonra da
    // «kimə getmişdi» sualına cavab qalsın.
    public class NotificationCampaign : IEntity, ISoftDelete
    {
        public int Id { get; set; }

        public int CreatedByUserId { get; set; }
        public User? CreatedBy { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? Link { get; set; }

        // Auditoriya şərtləri JSON olaraq — sahələr artdıqca miqrasiya lazım olmasın
        public string AudienceJson { get; set; } = "{}";

        // İnsan üçün oxunaqlı izah: «Müştərilər · Bakı · son 30 gündə sorğu göndərənlər»
        public string AudienceSummary { get; set; } = string.Empty;

        public NotificationChannels Channels { get; set; } = NotificationChannels.InApp;

        public CampaignStatus Status { get; set; } = CampaignStatus.Draft;

        // Boşdursa dərhal göndərilir
        public DateTime? ScheduledAt { get; set; }
        public DateTime? SentAt { get; set; }

        // Nəticə göstəriciləri
        public int RecipientCount { get; set; }
        public int ReadCount { get; set; }
        public int ClickCount { get; set; }

        // Göndərilə bilmədisə səbəb
        public string? Error { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
