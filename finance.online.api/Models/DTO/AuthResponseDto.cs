namespace finance.online.api.Models.DTO
{
    public class AuthResponseDto
    {
        public string AccessToken { get; set; } = null!;
        public DateTime AccessTokenExpiresAt { get; set; }
    }
}
