namespace Entities.Dtos.Staff
{
    // Saray rəhbərliyi öz sarayı üçün administrator hesabı yaradır
    public class CreateVenueStaffDto
    {
        public int VenueId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        // Boş buraxılarsa administrator sarayın bütün zallarında işləyə bilir
        public List<int>? HallIds { get; set; }
    }
}
