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

        // "rating-desc" (default), "price-asc", "price-desc"
        public string? Sort { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }
}
