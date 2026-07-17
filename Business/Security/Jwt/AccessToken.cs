namespace Business.Security.Jwt
{
    // Uğurlu login sonrası istifadəçiyə qaytarılan token obyekti
    public class AccessToken
    {
        public string Token { get; set; }
        public DateTime Expiration { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; }
    }
}
