namespace Entities.Dtos.Admin
{
    // SuperAdmin panelində istifadəçi siyahısı üçün
    public class AdminUserDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // Sahibdirsə neçə sarayı var
        public int VenueCount { get; set; }
    }

    // Platformanın ümumi göstəriciləri
    public class PlatformStatsDto
    {
        public int Users { get; set; }
        public int Customers { get; set; }
        public int Owners { get; set; }
        public int Venues { get; set; }
        public int PendingVenues { get; set; }
        public int Reservations { get; set; }
        public int PendingReservations { get; set; }
        public int Reviews { get; set; }
    }

    public class ChangeRoleDto
    {
        // "Customer", "Owner", "VenueAdmin", "SuperAdmin"
        public string Role { get; set; } = string.Empty;
    }
}
