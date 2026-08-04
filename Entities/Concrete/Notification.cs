using Core.Entities;

namespace Entities.Concrete
{
    // İstifadəçiyə göndərilmiş bir bildiriş.
    // Kanal fərqi yoxdur: e-poçt və ya SMS getsə də, sətir burada qalır —
    // istifadəçi saytda «mənə nə gəldi» sualının cavabını bir yerdə görsün.
    public class Notification : IEntity, ISoftDelete
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public NotificationType Type { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;

        // Saytda hansı səhifəyə aparır — məsələn /rezervlerim
        public string? Link { get; set; }

        // Elan göndərişindən yaranıbsa hansı göndərişdən
        public int? CampaignId { get; set; }
        public NotificationCampaign? Campaign { get; set; }

        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }

        // Linkə keçilibsə — elanın nəticəsini ölçmək üçün
        public DateTime? ClickedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
