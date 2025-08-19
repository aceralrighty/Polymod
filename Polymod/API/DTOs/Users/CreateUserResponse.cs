namespace PolyMod.API.DTOs.Users;

public abstract class CreateUserResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public UserDto? User { get; set; }
}
