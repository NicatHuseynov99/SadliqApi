namespace Entities.Dtos.Review
{
    public class UpdateReviewDto
    {
        // 1-5 arası bal
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
    }
}
