using Core.Entities;

namespace Entities.Concrete
{
    // sadlıq sarayı
    public class Venue : IEntity, ISoftDelete
    {
        public int Id { get; set; }
        public int OwnerId { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string Phone { get; set; } = string.Empty;

        // Neçə avtomobil yerləşir. Boş = məlumat verilməyib.
        // «Parkinq var» özəlliyindən fərqli məlumatdır: qonaq sayına uyğun olub-olmadığı
        // yalnız tutumdan bilinir.
        public int? ParkingCapacity { get; set; }

        // Sürücüsüz parklama xidməti
        public bool HasValetParking { get; set; }
        public string? CoverImageUrl { get; set; }

        // Sarayın təqdimat videosu: YouTube/Vimeo linki və ya birbaşa fayl.
        // Qalereyada birinci sırada göstərilir — video şəkildən güclü təsir bağışlayır.
        public string? VideoUrl { get; set; }
        public bool IsApproved { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Rədd səbəbi. Əvvəl yox idi: sahibkara «səbəbi öyrənmək üçün bizimlə
        // əlaqə saxlayın» yazılırdı — nəyi düzəldəcəyini bilmirdi.
        public string? RejectionReason { get; set; }
        public DateTime? RejectedAt { get; set; }
        public int? RejectedByUserId { get; set; }

        // Sahibkar düzəlişi edib yenidən yoxlamağa göndərib
        public DateTime? ResubmittedAt { get; set; }

        public ICollection<VenueImage> Images { get; set; } = new List<VenueImage>();
        public ICollection<Hall> Halls { get; set; } = new List<Hall>();
        public ICollection<Menu> Menus { get; set; } = new List<Menu>();
        public ICollection<VenueFeature> Features { get; set; } = new List<VenueFeature>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        // Silinmiş sətir saytda görünmür, amma bazada qalır — geri qaytarmaq olur
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
