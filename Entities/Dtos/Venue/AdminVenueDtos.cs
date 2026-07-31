namespace Entities.Dtos.Venue
{
    // Platforma üzrə saray axtarışının süzgəcləri
    public class AdminVenueSearchDto
    {
        // Ad, şəhər, ünvan və ya sahibin adı üzrə
        public string? Q { get; set; }
        public string? City { get; set; }

        // all | approved | pending
        public string? Status { get; set; }
        public int? OwnerId { get; set; }

        // name | newest | rating | reservations
        public string? Sort { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class AdminVenueDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // Redaktə pəncərəsi mövcud mətnlə açılsın deyə göndərilir
        public string Description { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CoverImageUrl { get; set; }
        public List<string> Features { get; set; } = new();
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }

        public int OwnerId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public string? OwnerEmail { get; set; }

        public int HallCount { get; set; }
        public int MenuCount { get; set; }
        public int ImageCount { get; set; }
        public int FeatureCount { get; set; }
        public int ReviewCount { get; set; }
        public double Rating { get; set; }

        public int ReservationCount { get; set; }
        public int PendingReservationCount { get; set; }
    }

    public class SetVenueApprovedDto
    {
        public bool IsApproved { get; set; }
    }

    public class ChangeVenueOwnerDto
    {
        public int NewOwnerId { get; set; }
    }
}
