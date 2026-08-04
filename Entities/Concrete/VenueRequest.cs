using Core.Entities;

namespace Entities.Concrete
{
    // Tərs axtarış: müştəri şərtlərini bir dəfə yazır, uyğun saraylar ona
    // təklif göndərir. 10 saraya zəng etmək əvəzinə 5 təklifi müqayisə edir.
    public class VenueRequest : IEntity, ISoftDelete
    {
        public int Id { get; set; }

        public int CustomerId { get; set; }
        public User? Customer { get; set; }

        public string City { get; set; } = string.Empty;

        // Tarix çevik ola bilər: «oktyabr ayı olsun» kimi
        public DateOnly? EventDate { get; set; }
        public bool IsDateFlexible { get; set; }

        public int GuestCount { get; set; }

        // Nəfər başına gözlənilən büdcə
        public decimal? BudgetPerPerson { get; set; }

        public bool NeedsParking { get; set; }
        public string? Note { get; set; }

        public VenueRequestStatus Status { get; set; } = VenueRequestStatus.Open;

        // Cavabsız sorğu əbədi açıq qalmasın
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }

        // Qəbul edilən təklifdən yaranan rezerv
        public int? ReservationId { get; set; }

        public ICollection<VenueOffer> Offers { get; set; } = new List<VenueOffer>();

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }

    // Sarayın həmin sorğuya cavabı
    public class VenueOffer : IEntity, ISoftDelete
    {
        public int Id { get; set; }

        public int RequestId { get; set; }
        public VenueRequest? Request { get; set; }

        public int VenueId { get; set; }
        public Venue? Venue { get; set; }

        public int HallId { get; set; }
        public Hall? Hall { get; set; }

        public int MenuId { get; set; }
        public Menu? Menu { get; set; }

        // Təklif olunan tarix: müştəri çevikdirsə saray özü gün seçir
        public DateOnly EventDate { get; set; }

        public decimal PricePerPerson { get; set; }
        public string Message { get; set; } = string.Empty;

        public int CreatedByUserId { get; set; }

        public VenueOfferStatus Status { get; set; } = VenueOfferStatus.Sent;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RespondedAt { get; set; }

        // Müştəri təklifi təsdiqləyib yazışmanı açıbsa. Rezerv hələ yaranmır —
        // əvvəlcə danışırlar, sonra müştəri qərar verir.
        public DateTime? ChatOpenedAt { get; set; }

        public ICollection<OfferMessage> Messages { get; set; } = new List<OfferMessage>();

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
