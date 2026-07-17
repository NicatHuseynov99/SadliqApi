namespace Business.Security.Jwt
{
    // appsettings.json-dan oxunan JWT konfiqurasiyası
    public class TokenOptions
    {
        public required string Audience { get; set; }
        public required string Issuer { get; set; }
        public int AccessTokenExpiration { get; set; }
        public required string SecurityKey { get; set; }
    }
}
