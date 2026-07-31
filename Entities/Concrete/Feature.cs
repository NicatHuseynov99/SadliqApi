using Core.Entities;

namespace Entities.Concrete
{
    // Özəllik kitabçası: parkinq, gəlin otağı, canlı musiqi...
    // Sarayın özəllikləri VenueFeatures cədvəlində qalır, bura isə seçim siyahısıdır.
    public class Feature : IEntity, ISoftDelete
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // İnterfeysdə göstərilən emoji və ya işarə (məcburi deyil)
        public string? Icon { get; set; }

        public int SortOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
