using Core.Entities;

namespace Entities.Concrete
{
    // 50/60/70 manatlıq menyular
    public class Menu : IEntity, ISoftDelete
    {
        public int Id { get; set; }
        public int VenueId { get; set; }
        public Venue? Venue { get; set; }

        public string Name { get; set; } = string.Empty;
        public decimal PricePerPerson { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }

        public ICollection<MenuItem> Items { get; set; } = new List<MenuItem>();

        // Silinmiş sətir saytda görünmür, amma bazada qalır — geri qaytarmaq olur
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
