using Core.Entities;

namespace Entities.Concrete
{
    // Təklif üzrə yazışma. Müştəri təklifi təsdiqləyəndən sonra açılır:
    // söhbət WhatsApp-a keçmir, platformada qalır — mübahisə olsa adminin
    // əlində yazışma olur, müştəri isə hər sarayla ayrıca lentdə danışır.
    public class OfferMessage : IEntity, ISoftDelete
    {
        public int Id { get; set; }

        public int OfferId { get; set; }
        public VenueOffer? Offer { get; set; }

        public int SenderUserId { get; set; }
        public User? Sender { get; set; }

        // Mesajı müştəri yazıb, yoxsa saray tərəfi
        public bool IsFromCustomer { get; set; }

        public string Body { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Qarşı tərəf oxuyubsa
        public DateTime? ReadAt { get; set; }

        // Moderasiya: qanunsuz və ya təhqiredici mesaj tərəflərdən gizlədilir,
        // amma silinmir — admin onu görməkdə davam edir, çünki mübahisədə dəlildir.
        public bool IsHidden { get; set; }
        public string? HiddenReason { get; set; }
        public DateTime? HiddenAt { get; set; }
        public int? HiddenByUserId { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
