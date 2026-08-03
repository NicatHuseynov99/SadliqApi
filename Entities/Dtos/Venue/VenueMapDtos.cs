namespace Entities.Dtos.Venue
{
    // Xəritə axtarışı: adi süzgəclərlə eynidir, üstəlik xəritənin görünən sahəsi.
    // Səhifələmə yoxdur — xəritə eyni anda bütün markerləri göstərməlidir,
    // ona görə nəticə sayı serverdə məhdudlaşdırılır.
    public class VenueMapSearchDto
    {
        public string? Q { get; set; }
        public string? City { get; set; }
        public DateOnly? Date { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? Capacity { get; set; }
        public decimal? Budget { get; set; }
        public double? MinRating { get; set; }
        public int? MinParking { get; set; }
        public List<string>? Features { get; set; }

        // Xəritənin görünən sahəsi (bounding box). Dördü də verilməlidir,
        // yoxsa süzgəc tətbiq olunmur.
        public double? North { get; set; }
        public double? South { get; set; }
        public double? East { get; set; }
        public double? West { get; set; }

        // «Mənə yaxın»: verilsə nəticə məsafəyə görə sıralanır
        public double? NearLat { get; set; }
        public double? NearLng { get; set; }

        // Xəritədə eyni anda göstəriləcək maksimum marker
        public int Limit { get; set; } = 200;
    }

    // Marker üçün yüngül cavab — saray kartının bütün sahələri lazım deyil
    public class VenueMapItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? CoverImageUrl { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public decimal? MinPrice { get; set; }
        public int? MaxCapacity { get; set; }
        public int? ParkingCapacity { get; set; }
        public double Rating { get; set; }
        public int ReviewCount { get; set; }

        // «Mənə yaxın» seçiləndə doldurulur (km)
        public double? DistanceKm { get; set; }
    }

    public class VenueMapResultDto
    {
        public List<VenueMapItemDto> Items { get; set; } = new();

        // Süzgəcə uyğun gələn ümumi say — limitdən çox ola bilər
        public int TotalMatched { get; set; }

        // Koordinatı olmadığı üçün xəritədə görünməyən saraylar
        public int WithoutCoordinates { get; set; }
    }
}
