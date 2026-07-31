using Core.Entities;

namespace Entities.Concrete
{
    // sadlıq sarayı
    public class Venue : IEntity, ISoftDelete
    {
        public int Id { get; set; }
        public int OwnerId { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string? CoverImageUrl { get; set; }
        public bool IsApproved { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<VenueImage> Images { get; set; } = new List<VenueImage>();
        public ICollection<Hall> Halls { get; set; } = new List<Hall>();
        public ICollection<Menu> Menus { get; set; } = new List<Menu>();
        public ICollection<VenueFeature> Features { get; set; } = new List<VenueFeature>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        // Silinmiş sətir saytda görünmür, amma bazada qalır — geri qaytarmaq olur
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
