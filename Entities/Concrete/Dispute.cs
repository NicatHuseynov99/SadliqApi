using Core.Entities;

namespace Entities.Concrete
{
    // Şikayət/mübahisə işi. Əvvəl belə bir kanal yox idi: müştərinin saraydan
    // şikayət etmək üçün yeganə yolu rəyi şikayət etmək idi, sarayın isə
    // problemli müştərini bildirmək imkanı ümumiyyətlə yox idi.
    //
    // Admin müdaxiləsi (rezervin statusunu kənardan dəyişmək) mövcud idi, amma
    // izsiz və birtərəfli — nə iki tərəfin izahı, nə iş qeydi qalırdı.
    public class Dispute : IEntity, ISoftDelete
    {
        public int Id { get; set; }

        // İşi açan tərəf
        public int OpenedByUserId { get; set; }
        public User? OpenedBy { get; set; }

        // Müştəri açıbsa true, saray tərəfi açıbsa false —
        // dəstək kimin şikayət etdiyini dərhal görməlidir
        public bool OpenedByCustomer { get; set; }

        // Şikayət nəyin üstündədir. Rezerv göstərilibsə saray da ondan çıxarılır.
        public int? ReservationId { get; set; }
        public Reservation? Reservation { get; set; }

        public int? VenueId { get; set; }
        public Venue? Venue { get; set; }

        public DisputeCategory Category { get; set; } = DisputeCategory.Other;

        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public DisputeStatus Status { get; set; } = DisputeStatus.Open;

        // İşi öz üzərinə götürən dəstək işçisi
        public int? AssignedToUserId { get; set; }
        public User? AssignedTo { get; set; }

        // Qərar və izahı — iş bağlananda doldurulur
        public string? Resolution { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public int? ResolvedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Yazışma: tərəflər və dəstək eyni lentdə danışır
        public ICollection<DisputeMessage> Messages { get; set; } = new List<DisputeMessage>();

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }

    // İş üzrə yazışma. Adminin konkret istifadəçiyə mesaj yazması üçün ayrıca
    // kanal qurmaq əvəzinə söhbət işə bağlanır — beləliklə kontekst itmir və
    // qərar hansı danışıqdan sonra verildiyi görünür.
    public class DisputeMessage : IEntity, ISoftDelete
    {
        public int Id { get; set; }

        public int DisputeId { get; set; }
        public Dispute? Dispute { get; set; }

        public int SenderUserId { get; set; }
        public User? Sender { get; set; }

        // Platforma işçisinin mesajı tərəflərinkindən fərqlənməlidir
        public bool IsFromStaff { get; set; }

        // Yalnız dəstək üçün qeyd: tərəflər görmür
        public bool IsInternalNote { get; set; }

        public string Body { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
