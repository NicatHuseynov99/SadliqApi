namespace Entities.Dtos.Hall
{
    public class UpdateHallDto
    {
        public string Name { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal MinPrice { get; set; }

        // Media yükləmə hazır olana qədər zalın şəkli linklə verilir
        public string? ImageUrl { get; set; }
    }
}
