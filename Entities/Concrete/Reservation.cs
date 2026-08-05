using Core.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Concrete
{
    // rezerv sorğuları
    public class Reservation : IEntity, ISoftDelete
    {
        public int Id { get; set; }

        public int CustomerId { get; set; }
        public User? Customer { get; set; }

        public int VenueId { get; set; }
        public Venue? Venue { get; set; }

        public int HallId { get; set; }
        public Hall? Hall { get; set; }

        public int MenuId { get; set; }
        public Menu? Menu { get; set; }

        public DateOnly EventDate { get; set; }
        public int GuestCount { get; set; }
        public string ContactPhone { get; set; } = string.Empty;
        public string? Note { get; set; }
        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Sahibə xatırlatma göndərilibsə — bir dəfədən çox göndərilməsin deyə
        public DateTime? OwnerRemindedAt { get; set; }

        // Tədbirdən əvvəl xatırlatma göndərildi
        public DateTime? EventReminderSentAt { get; set; }

        // Tədbirdən sonra rəy dəvəti göndərildi
        public DateTime? ReviewInviteSentAt { get; set; }

        // Tarixin tutulma müddəti — bazada zalın təqvimində saxlanılır,
        // müştəriyə «cavaba nə qədər qalıb» göstərmək üçün siyahıda doldurulur
        [NotMapped]
        public DateTime? HoldExpiresAt { get; set; }

        // Sahib sorğunu ilk dəfə nə vaxt açıb. Müştəri «baxıldı» görsün deyə:
        // gözləmək pisdir, nə gözlədiyini bilməmək daha pisdir.
        public DateTime? SeenAt { get; set; }

        // Saray tərəfindən kim açıb: sahibin özü, yoxsa hansı administrator.
        // Platforma işçisi (admin, dəstək) baxanda buraya heç nə yazılmır —
        // müştəri üçün «saray baxıb» yalnız sarayın öz adamı deməkdir.
        public int? SeenByUserId { get; set; }
        public User? SeenBy { get; set; }

        // Əks-təkliflərin tarixçəsi
        public ICollection<ReservationCounterOffer> CounterOffers { get; set; }
            = new List<ReservationCounterOffer>();

        // Silinmiş sətir saytda görünmür, amma bazada qalır — geri qaytarmaq olur
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
