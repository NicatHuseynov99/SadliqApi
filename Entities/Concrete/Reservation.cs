using Core.Entities;

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

        // Silinmiş sətir saytda görünmür, amma bazada qalır — geri qaytarmaq olur
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
