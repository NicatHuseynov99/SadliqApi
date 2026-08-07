using Core.Entities;

namespace Entities.Concrete
{
    // saray haqqında qonaq rəyi
    public class Review : IEntity, ISoftDelete
    {
        public int Id { get; set; }
        public int VenueId { get; set; }
        public Venue? Venue { get; set; }

        // Rəyi yazan qeydiyyatlı istifadəçidirsə saxlanılır (köçürülmüş rəylərdə boş ola bilər)
        public int? CustomerId { get; set; }
        public User? Customer { get; set; }

        public string AuthorName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Moderasiya: gizlədilmiş rəy saytda görünmür və reytinqə təsir etmir,
        // amma silinmir — lazım olsa geri qaytarıla bilər.
        public bool IsHidden { get; set; }
        public string? HiddenReason { get; set; }
        public DateTime? HiddenAt { get; set; }
        public int? HiddenByUserId { get; set; }

        // Şikayət sayı — moderatorun diqqətini yönəltmək üçün saxlanılır
        public int ReportCount { get; set; }

        // Sarayın rəyə açıq cavabı. Bir rəyə bir cavab olur, redaktə edilə bilir.
        // Şikayəti cavabsız qoymaq sarayın özünə ziyandır: oxuyan yalnız
        // bir tərəfi görür.
        public string? OwnerReply { get; set; }
        public DateTime? OwnerRepliedAt { get; set; }

        // Cavabı saray tərəfindən kim yazıb — sahibin özü, administrator
        // və ya (dəstək məqsədilə) platforma işçisi
        public int? OwnerReplyByUserId { get; set; }

        // Rəy həqiqətən orada olan qonaqdandırsa: təsdiqlənmiş və tarixi keçmiş
        // rezervi var. Yalnız belə rəylər reytinq ortalamasına düşür — yoxsa
        // reytinq heç nə ifadə etmir.
        public bool IsVerifiedGuest { get; set; }

        // Hansı rezervdən yarandığı — bir tədbirə bir rəy qaydası üçün
        public int? ReservationId { get; set; }

        // Silinmiş sətir saytda görünmür, amma bazada qalır — geri qaytarmaq olur
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
