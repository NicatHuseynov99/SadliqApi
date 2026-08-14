namespace Entities.Dtos.Admin
{
    // Admin yazışma siyahısının süzgəcləri
    public class AdminChatQueryDto
    {
        // Saray adı, müştəri adı və ya mesajın mətni üzrə axtarış
        public string? Search { get; set; }

        public int? VenueId { get; set; }
        public int? CustomerId { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    // Siyahıda bir yazışmanın xülasəsi — adminin hansını açacağını seçməsi üçün
    public class AdminChatSummaryDto
    {
        // Yazışmanın kökü: «Offer» — sorğu/təklif axını, «Reservation» — sarayın
        // səhifəsindən birbaşa edilmiş rezerv. Adminin əlində hər ikisi olmalıdır,
        // çünki mübahisə hansı axından gəldiyinə baxmır.
        public string ThreadKind { get; set; } = "Offer";

        // Növündən asılı olaraq biri dolu olur
        public int? OfferId { get; set; }
        public int? RequestId { get; set; }
        public int? ReservationId { get; set; }

        public int VenueId { get; set; }
        public string VenueName { get; set; } = string.Empty;

        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }
        public int GuestCount { get; set; }

        public DateTime ChatOpenedAt { get; set; }
        public int MessageCount { get; set; }

        public DateTime? LastMessageAt { get; set; }
        public string LastMessagePreview { get; set; } = string.Empty;
        public bool LastMessageFromCustomer { get; set; }
    }

    // Yazışmanın tam görünüşü: kimlər danışır, hansı təklif üzrə, bütün mesajlar
    public class AdminChatDetailDto
    {
        public string ThreadKind { get; set; } = "Offer";

        public int? OfferId { get; set; }
        public int? RequestId { get; set; }
        public int? ReservationId { get; set; }

        public string VenueName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }
        public int GuestCount { get; set; }
        public decimal PricePerPerson { get; set; }

        // Təklif lentində təklifin, rezerv lentində rezervin vəziyyəti
        public string OfferStatus { get; set; } = string.Empty;

        public DateTime ChatOpenedAt { get; set; }

        public List<AdminChatMessageDto> Messages { get; set; } = new();
    }

    public class AdminChatMessageDto
    {
        public int Id { get; set; }
        public bool IsFromCustomer { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }

        // Gizlədilmiş mesaj tərəflərə görünmür, admin isə görür
        public bool IsHidden { get; set; }
        public string? HiddenReason { get; set; }
        public DateTime? HiddenAt { get; set; }
    }

    // Mesajın gizlədilməsi və ya yenidən göstərilməsi
    public class SetMessageHiddenDto
    {
        public bool IsHidden { get; set; }

        // Gizlədəndə məcburidir — nəyə görə çıxarıldığı qeyd olunmalıdır
        public string? Reason { get; set; }
    }
}
