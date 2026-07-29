namespace Entities.Dtos.Auth
{
    public class ResetPasswordDto
    {
        // E-poçtdakı linkdən gələn birdəfəlik token
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
