using Core.Entities;

namespace Entities.Concrete
{
    // Şəhər kitabçası. Əvvəl şəhər adı yalnız sarayın öz sahəsində mətn kimi
    // saxlanılırdı — o zaman «yeni şəhər əlavə et» və ya «boş şəhəri sil» mümkün deyildi.
    public class City : IEntity, ISoftDelete
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // Siyahıda yuxarıda görünsün deyə (kiçik rəqəm — daha öndə)
        public int SortOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
