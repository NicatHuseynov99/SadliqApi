using Core.Entities;

namespace Entities.Concrete
{
    // administratorun hansı zallarda işləyə biləcəyi.
    // Heç bir sətir yoxdursa, administrator sarayın bütün zallarına baxa bilir.
    public class VenueStaffHall : IEntity
    {
        public int Id { get; set; }

        public int VenueStaffId { get; set; }
        public VenueStaff? VenueStaff { get; set; }

        public int HallId { get; set; }
        public Hall? Hall { get; set; }
    }
}
