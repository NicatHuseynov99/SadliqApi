namespace Entities.Dtos.Owner
{
    // Sahibkarın öz sarayı üzrə mənzərəsi: baxışdan rezervə qədər.
    // Məqsəd rəqəm göstərmək deyil — nə etməli olduğunu göstərməkdir.
    public class OwnerStatsDto
    {
        public int VenueId { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public int Days { get; set; }

        // Səhifə baxışları
        public int Views { get; set; }
        public int UniqueVisitors { get; set; }

        // Əvvəlki eyni müddətlə müqayisə (faiz; mənfi = azalıb)
        public int ViewsChangePercent { get; set; }

        // Seçilmişlərə əlavə edənlər
        public int Favorites { get; set; }

        // Sorğu axını
        public int Requests { get; set; }
        public int Confirmed { get; set; }
        public int Rejected { get; set; }
        public int Expired { get; set; }
        public int Pending { get; set; }

        // Baxışdan sorğuya və sorğudan təsdiqə çevrilmə
        public int ViewToRequestPercent { get; set; }
        public int ConfirmPercent { get; set; }

        // Orta cavab müddəti (saat). Cavablanmış sorğu yoxdursa boşdur.
        public double? AverageResponseHours { get; set; }

        // Cavabsız qalıb vaxtı keçən sorğular — birbaşa itirilmiş gəlirdir
        public decimal LostIncome { get; set; }
        public decimal ExpectedIncome { get; set; }

        public double? Rating { get; set; }
        public int ReviewCount { get; set; }

        // Tərs axtarış
        public int OffersSent { get; set; }
        public int OffersAccepted { get; set; }

        // «Nə etsən yaxşı olar» siyahısı
        public List<string> Advice { get; set; } = new();
    }
}
