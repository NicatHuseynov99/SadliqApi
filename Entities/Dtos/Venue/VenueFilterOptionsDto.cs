namespace Entities.Dtos.Venue
{
    // Ana səhifədəki filtr panelinin seçimləri — bazadakı real dataya görə qurulur
    public class VenueFilterOptionsDto
    {
        public List<FilterOption> Cities { get; set; } = new();
        public List<FilterOption> Features { get; set; } = new();

        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public int MinCapacity { get; set; }
        public int MaxCapacity { get; set; }
    }

    // Seçim adı və neçə sarayda olduğu (filtrin yanında say göstərmək üçün)
    public class FilterOption
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
