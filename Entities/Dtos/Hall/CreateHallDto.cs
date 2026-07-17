namespace Entities.Dtos.Hall
{
    public class CreateHallDto
    {
        public int VenueId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal MinPrice { get; set; }
    }
}
