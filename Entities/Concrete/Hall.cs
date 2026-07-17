using Core.Entities;

namespace Entities.Concrete
{
    // zallar
    public class Hall : IEntity
    {
        public int Id { get; set; }
        public int VenueId { get; set; }
        public Venue? Venue { get; set; }

        public string Name { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal MinPrice { get; set; }

        public ICollection<HallMedia> Media { get; set; } = new List<HallMedia>();
        public ICollection<HallAvailability> Availability { get; set; } = new List<HallAvailability>();
    }
}
