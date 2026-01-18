namespace OLDBRICK_STANJE_ARTIKALA_APP.DTOs.Auth
{
    public class LoginResponseDto
    {
        public long UserId { get; set; }
        public string? Username { get; set; }
        public string? Role { get; set; }
        public string? Token { get; set; }


    }
}
