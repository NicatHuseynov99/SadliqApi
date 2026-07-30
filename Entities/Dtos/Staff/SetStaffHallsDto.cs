namespace Entities.Dtos.Staff
{
    // Administratorun işləyə biləcəyi zalları təyin edir.
    // Boş siyahı göndərilsə, məhdudiyyət götürülür (bütün zallar).
    public class SetStaffHallsDto
    {
        public List<int> HallIds { get; set; } = new();
    }
}
