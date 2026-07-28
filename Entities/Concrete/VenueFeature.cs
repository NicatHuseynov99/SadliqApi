using Core.Entities;

namespace Entities.Concrete
{
    // sarayın özəllikləri: parkinq, gəlin otağı, canlı musiqi...
    public class VenueFeature : IEntity
    {
        public int Id { get; set; }
        public int VenueId { get; set; }
        public Venue? Venue { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
