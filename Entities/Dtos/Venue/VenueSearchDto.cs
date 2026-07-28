namespace Entities.Dtos.Venue
{
    // Ana səhifənin axtarış/filtr/səhifələmə parametrləri
    public class VenueSearchDto
    {
        public string? Q { get; set; }          // ad və ya ünvan üzrə axtarış
        public string? City { get; set; }
        public DateOnly? Date { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? Capacity { get; set; }

        // Seçilmiş özəlliklər — sarayda hamısı olmalıdır (parkinq, gəlin otağı...)
        public List<string>? Features { get; set; }

        // Müştərinin ümumi büdcəsi: menyu qiyməti × qonaq sayı bu məbləği aşmamalıdır
        public decimal? Budget { get; set; }

        // Minimum orta reytinq (məsələn 4.5)
        public double? MinRating { get; set; }

        // "rating-desc" (default), "price-asc", "price-desc"
        public string? Sort { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }
}
