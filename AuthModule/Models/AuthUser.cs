using TBD.GenericDBProperties;

namespace TBD.AuthModule.Models;

public class AuthUser: DateableObject
{
    public Guid Id { get; set; }

    // Core user info (duplicated for auth context)
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string HashedPassword { get; set; }

    // Auth-specific properties
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    public DateTime? LastLogin { get; set; }
    public int FailedLoginAttempts { get; set; }
}
