using Entities.Concrete;

namespace Entities.Dtos.VenueRequest
{
    // Müştərinin doldurduğu qısa forma
    public class CreateVenueRequestDto
    {
        public string City { get; set; } = string.Empty;
        public DateOnly? EventDate { get; set; }
        public bool IsDateFlexible { get; set; }
        public int GuestCount { get; set; }
        public decimal? BudgetPerPerson { get; set; }
        public bool NeedsParking { get; set; }
        public string? Note { get; set; }
    }

    // Sarayın cavabı
    public class CreateVenueOfferDto
    {
        public int RequestId { get; set; }
        public int VenueId { get; set; }
        public int HallId { get; set; }
        public int MenuId { get; set; }
        public DateOnly EventDate { get; set; }
        public decimal PricePerPerson { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // Müştərinin gördüyü təklif — müqayisə üçün lazım olan hər şey bir yerdə
    public class VenueOfferDto
    {
        public int Id { get; set; }
        public int VenueId { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public string? VenueImageUrl { get; set; }
        public string City { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        public string HallName { get; set; } = string.Empty;
        public int HallCapacity { get; set; }
        public string MenuName { get; set; } = string.Empty;

        public DateOnly EventDate { get; set; }
        public decimal PricePerPerson { get; set; }
        public decimal TotalPrice { get; set; }
        public string Message { get; set; } = string.Empty;

        // Müqayisəni asanlaşdıran göstəricilər
        public double? Rating { get; set; }
        public int ReviewCount { get; set; }
        public int? ParkingCapacity { get; set; }

        public VenueOfferStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }

        // Yazışma açılıbmı və oxunmamış mesaj varmı
        public bool IsChatOpen { get; set; }
        public int UnreadCount { get; set; }
        public string? LastMessage { get; set; }
        public DateTime? LastMessageAt { get; set; }
    }

    public class OfferMessageDto
    {
        public int Id { get; set; }
        public bool IsFromCustomer { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }

    public class SendMessageDto
    {
        public int OfferId { get; set; }
        public string Body { get; set; } = string.Empty;
    }

    // Sahibin göndərdiyi təkliflərin siyahısı — yazışmalar buradan açılır
    public class MyOfferDto
    {
        public int Id { get; set; }
        public int RequestId { get; set; }
        public int VenueId { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;
        public int GuestCount { get; set; }
        public DateOnly EventDate { get; set; }
        public decimal PricePerPerson { get; set; }
        public decimal TotalPrice { get; set; }

        public VenueOfferStatus Status { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public VenueRequestStatus RequestStatus { get; set; }

        public bool IsChatOpen { get; set; }
        public int UnreadCount { get; set; }
        public string? LastMessage { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class VenueRequestDto
    {
        public int Id { get; set; }
        public string City { get; set; } = string.Empty;
        public DateOnly? EventDate { get; set; }
        public bool IsDateFlexible { get; set; }
        public int GuestCount { get; set; }
        public decimal? BudgetPerPerson { get; set; }
        public bool NeedsParking { get; set; }
        public string? Note { get; set; }

        public VenueRequestStatus Status { get; set; }
        public string StatusLabel { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int? ReservationId { get; set; }

        public List<VenueOfferDto> Offers { get; set; } = new();
    }

    // Sahibin gördüyü açıq sorğu
    public class OpenRequestDto
    {
        public int Id { get; set; }
        public string City { get; set; } = string.Empty;
        public DateOnly? EventDate { get; set; }
        public bool IsDateFlexible { get; set; }
        public int GuestCount { get; set; }
        public decimal? BudgetPerPerson { get; set; }
        public bool NeedsParking { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }

        // Sorğuya neçə saray cavab verib — sahib rəqabəti görsün
        public int OfferCount { get; set; }

        // Bu sahibin hansı sarayları uyğun gəlir
        public List<MatchingVenueDto> MatchingVenues { get; set; } = new();

        // Artıq təklif göndərilibsə hansı saraydan
        public List<int> OfferedVenueIds { get; set; } = new();
    }

    public class MatchingVenueDto
    {
        public int VenueId { get; set; }
        public string VenueName { get; set; } = string.Empty;

        // Yalnız qonaq sayına yetən zallar
        public List<HallOptionDto> Halls { get; set; } = new();
        public List<MenuOptionDto> Menus { get; set; } = new();
    }

    public class HallOptionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Capacity { get; set; }
    }

    public class MenuOptionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal PricePerPerson { get; set; }
    }
}
