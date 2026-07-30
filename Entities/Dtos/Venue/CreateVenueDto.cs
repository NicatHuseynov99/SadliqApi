namespace Entities.Dtos.Venue
{
    public class CreateVenueDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string Phone { get; set; } = string.Empty;

        // Media yükləmə hazır olana qədər şəkil linklə verilir
        public string? CoverImageUrl { get; set; }

        // Parkinq, gəlin otağı, canlı musiqi...
        public List<string>? Features { get; set; }
    }
}
