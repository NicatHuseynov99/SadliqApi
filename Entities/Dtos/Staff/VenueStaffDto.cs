namespace Entities.Dtos.Staff
{
    // Siyahıda göstərmək üçün — parol məlumatları heç vaxt qaytarılmır
    public class VenueStaffDto
    {
        public int Id { get; set; }
        public int VenueId { get; set; }
        public string VenueName { get; set; } = string.Empty;

        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // Boşdursa administrator sarayın bütün zallarına baxa bilir
        public List<int> HallIds { get; set; } = new();
        public List<string> HallNames { get; set; } = new();
    }
}
