using Core.Entities;

namespace Entities.Concrete
{
    // zalın şəkil və videoları
    public class HallMedia : IEntity, ISoftDelete
    {
        public int Id { get; set; }
        public int HallId { get; set; }
        public Hall? Hall { get; set; }

        public string Url { get; set; } = string.Empty;
        public MediaType MediaType { get; set; } = MediaType.Image;
        public int SortOrder { get; set; } = 0;

        // Silinmiş sətir saytda görünmür, amma bazada qalır — geri qaytarmaq olur
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
