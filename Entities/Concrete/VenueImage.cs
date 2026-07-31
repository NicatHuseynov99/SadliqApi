using Core.Entities;

namespace Entities.Concrete
{
    // sarayın ön və digər şəkilləri
    public class VenueImage : IEntity, ISoftDelete
    {
        public int Id { get; set; }
        public int VenueId { get; set; }
        public Venue? Venue { get; set; }

        public string Url { get; set; } = string.Empty;
        public bool IsCover { get; set; } = false;
        public int SortOrder { get; set; } = 0;

        // Silinmiş sətir saytda görünmür, amma bazada qalır — geri qaytarmaq olur
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
