using Core.Entities;

namespace Entities.Concrete
{
    // saray rəhbərliyinin təyin etdiyi administrator — hansı istifadəçinin hansı saraya aid olduğunu bildirir
    public class VenueStaff : IEntity
    {
        public int Id { get; set; }

        public int VenueId { get; set; }
        public Venue? Venue { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        // Təyinatı kim etdi (audit üçün)
        public int CreatedByUserId { get; set; }

        // Silmək əvəzinə deaktiv edilir ki, kimin nəyi təsdiqlədiyi tarixçədə qalsın
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Boşdursa sarayın bütün zalları, doludursa yalnız seçilmiş zallar
        public ICollection<VenueStaffHall> Halls { get; set; } = new List<VenueStaffHall>();
    }
}
