using Core.Entities;

namespace Entities.Concrete
{
    // hansı günlər dolu/boş
    public class HallAvailability : IEntity
    {
        public int Id { get; set; }
        public int HallId { get; set; }
        public Hall? Hall { get; set; }

        public DateOnly Date { get; set; }
        public AvailabilityStatus Status { get; set; } = AvailabilityStatus.Available;

        // «Held» vəziyyətinin son tarixi. Sahib bu vaxta qədər cavab verməsə,
        // gün öz-özünə boşalır — yoxsa cavabsız bir sorğu tarixi əbədi bağlayır.
        public DateTime? HoldExpiresAt { get; set; }
    }
}
