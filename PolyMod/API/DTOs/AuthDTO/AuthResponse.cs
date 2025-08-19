namespace TBD.API.DTOs.AuthDTO;

public class AuthResponse
{
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public string? Username { get; set; }
    public bool IsSuccessful { get; set; }
    public string? Message { get; set; }
}
