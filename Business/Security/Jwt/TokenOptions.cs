namespace Business.Security.Jwt
{
    // appsettings.json-dan oxunan JWT konfiqurasiyası
    public class TokenOptions
    {
        public string Audience { get; set; }
        public string Issuer { get; set; }
        public int AccessTokenExpiration { get; set; }
        public string SecurityKey { get; set; }
    }
}
