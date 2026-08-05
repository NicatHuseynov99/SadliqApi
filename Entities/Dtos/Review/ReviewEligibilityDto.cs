namespace Entities.Dtos.Review
{
    // İstifadəçi bu saraya rəy yaza bilərmi və yaza bilmirsə niyə
    public class ReviewEligibilityDto
    {
        public bool CanReview { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
