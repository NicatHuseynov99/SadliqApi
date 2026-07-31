namespace Entities.Dtos.Platform
{
    // Paneldə göstərilən parametr: dəyərlə birlikdə nə işə yaradığı da gedir
    public class SettingDto
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // number | text — panel uyğun sahə göstərsin deyə
        public string Type { get; set; } = "text";
        public DateTime? UpdatedAt { get; set; }
    }

    public class UpdateSettingDto
    {
        public string Value { get; set; } = string.Empty;
    }

    // Şəhər və ya özəllik — neçə sarayda işlənir
    public class ReferenceItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public int SortOrder { get; set; }
        public int VenueCount { get; set; }
        public bool IsDeleted { get; set; }
    }

    // İnterfeys mətni: açar kodda işlənir, dəyər isə ekranda görünür
    public class AppTextDto
    {
        public int Id { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Extra { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class AppTextFormDto
    {
        public string Category { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Extra { get; set; }
    }

    // Kateqoriyanın adı və neçə mətn saxladığı
    public class TextCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Count { get; set; }

        // Bu kateqoriyada ikinci dəyər (Extra) istifadə olunurmu
        public bool UsesExtra { get; set; }
    }

    // Yeni şəhər / özəllik əlavə etmək və mövcudu redaktə etmək
    public class ReferenceFormDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public int SortOrder { get; set; }
    }
}
