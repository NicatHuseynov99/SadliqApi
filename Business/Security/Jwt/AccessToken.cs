namespace Business.Security.Jwt
{
    // Uğurlu login sonrası istifadəçiyə qaytarılan token obyekti
    public class AccessToken
    {
        public required string Token { get; set; }
        public DateTime Expiration { get; set; }
        public int UserId { get; set; }
        public required string FullName { get; set; }
    }
}
