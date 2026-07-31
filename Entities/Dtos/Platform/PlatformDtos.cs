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
        public string Name { get; set; } = string.Empty;
        public int VenueCount { get; set; }
    }

    public class RenameReferenceDto
    {
        public string OldName { get; set; } = string.Empty;
        public string NewName { get; set; } = string.Empty;
    }
}
