namespace Entities.Dtos.Review
{
    // Saray tərəfinin öz saraylarına gələn rəyləri. Əvvəl belə siyahı yox idi:
    // sahib cavab yazmaq üçün hər sarayın ictimai səhifəsinə keçməli idi.
    public class OwnerReviewDto
    {
        public int Id { get; set; }

        public int VenueId { get; set; }
        public string VenueName { get; set; } = string.Empty;

        public string AuthorName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // Tədbiri keçmiş qonaqdan gələn rəy daha ağırdır
        public bool IsVerifiedGuest { get; set; }

        public string? OwnerReply { get; set; }
        public DateTime? OwnerRepliedAt { get; set; }
    }
}
