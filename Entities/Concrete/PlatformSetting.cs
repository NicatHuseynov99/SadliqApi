using Core.Entities;

namespace Entities.Concrete
{
    // Kodda sabit yazmaq istəmədiyimiz platforma parametrləri: açar-dəyər cütü.
    // Yalnız həqiqətən istifadə olunan açarlar saxlanılır — «gələcəkdə lazım olar»
    // deyə boş sətir yığmırıq.
    public class PlatformSetting : IEntity
    {
        public int Id { get; set; }

        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public int? UpdatedByUserId { get; set; }
    }
}
