using Core.Entities;

namespace Entities.Concrete
{
    // müştərinin seçilmişlərə (ürək) əlavə etdiyi saray
    public class Favorite : IEntity
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public int VenueId { get; set; }
        public Venue? Venue { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
