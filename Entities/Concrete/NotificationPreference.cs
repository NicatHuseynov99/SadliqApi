using Core.Entities;

namespace Entities.Concrete
{
    // İstifadəçinin bildiriş seçimləri.
    // Sistem bildirişləri (rezervin statusu, tarixin bağlanması) həmişə gedir —
    // onlar xidmətin bir hissəsidir. Söndürülə bilən yalnız elanlardır.
    public class NotificationPreference : IEntity
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        // Elan xarakterli bildirişlər
        public bool AllowAnnouncements { get; set; } = true;

        // Kanallar: sistem bildirişləri üçün də keçərlidir
        public bool AllowEmail { get; set; } = true;
        public bool AllowSms { get; set; } = true;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
