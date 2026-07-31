using Core.Entities;

namespace Entities.Concrete
{
    // İstifadəçinin bir rəy haqqında şikayəti. Rəy silinsə şikayət də silinir.
    public class ReviewReport : IEntity
    {
        public int Id { get; set; }

        public int ReviewId { get; set; }
        public Review? Review { get; set; }

        // Şikayəti yazan istifadəçi. Hesab silinsə şikayət qalır, sahibi boşalır.
        public int? ReporterUserId { get; set; }
        public User? ReporterUser { get; set; }

        // Hazır səbəb açarı: spam, offensive, fake, irrelevant, other
        public string Reason { get; set; } = string.Empty;

        // İstifadəçinin əlavə izahı (məcburi deyil)
        public string? Note { get; set; }

        // Moderator baxıb qərar verəndə bağlanır
        public bool IsResolved { get; set; }
        public DateTime? ResolvedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
