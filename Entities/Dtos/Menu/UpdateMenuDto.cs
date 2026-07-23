namespace Entities.Dtos.Menu
{
    public class UpdateMenuDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal PricePerPerson { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
