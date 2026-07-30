namespace Entities.Dtos.Menu
{
    public class CreateMenuDto
    {
        public int VenueId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal PricePerPerson { get; set; }
        public string Description { get; set; } = string.Empty;

        // Süfrənin şəkli (media yükləmə hazır olana qədər link)
        public string? ImageUrl { get; set; }
        public List<CreateMenuItemDto> Items { get; set; } = new();
    }

    public class CreateMenuItemDto
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }
}
