namespace Entities.Dtos.Admin
{
    // Admin panelində istifadəçi siyahısı üçün
    public class AdminUserDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // Sahibdirsə neçə sarayı var
        public int VenueCount { get; set; }
    }

    // Bir istifadəçinin tam mənzərəsi — admin panelində detal pəncərəsi
    public class AdminUserDetailDto
    {
        public AdminUserDto User { get; set; } = new();
        public int ReservationCount { get; set; }
        public int PendingReservationCount { get; set; }
        public int ReviewCount { get; set; }
        public DateTime? LastReservationAt { get; set; }

        // Sahib və ya administrator olduğu sarayların adları
        public List<string> Venues { get; set; } = new();
    }

    // Platformanın ümumi göstəriciləri
    public class PlatformStatsDto
    {
        public int Users { get; set; }
        public int Customers { get; set; }
        public int Owners { get; set; }
        public int BlockedUsers { get; set; }
        public int Venues { get; set; }
        public int PendingVenues { get; set; }
        public int Reservations { get; set; }
        public int PendingReservations { get; set; }
        public int Reviews { get; set; }
    }

    public class ChangeRoleDto
    {
        // "Customer", "Owner", "VenueAdmin", "Support", "SuperAdmin"
        public string Role { get; set; } = string.Empty;
    }

    // Hesabın bloklanması / açılması
    public class SetUserActiveDto
    {
        public bool IsActive { get; set; }
    }

    // Admin tərəfindən parolun sıfırlanması
    public class AdminResetPasswordDto
    {
        public string NewPassword { get; set; } = string.Empty;
    }
}
