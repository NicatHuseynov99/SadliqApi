using Core.Entities;

namespace Entities.Concrete
{
    // parol sıfırlama linki üçün birdəfəlik token
    public class PasswordResetToken : IEntity
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        // Token-in özü yox, SHA256 hash-i saxlanılır — baza sızsa belə link işə yaramasın
        public string TokenHash { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }
        public DateTime? UsedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
