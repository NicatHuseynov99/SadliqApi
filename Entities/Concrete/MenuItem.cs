using Core.Entities;

namespace Entities.Concrete
{
    // menyunun tərkibi
    public class MenuItem : IEntity
    {
        public int Id { get; set; }
        public int MenuId { get; set; }
        public Menu? Menu { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // horaji, isti, şirniyyat...
        public string Description { get; set; } = string.Empty;
    }
}
