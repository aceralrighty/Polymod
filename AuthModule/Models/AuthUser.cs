using System.ComponentModel.DataAnnotations;
using TBD.Shared.GenericDBProperties;

namespace TBD.AuthModule.Models;

public class AuthUser : BaseTableProperties
{
    // Core user info (duplicated for auth context)
    [MaxLength(255)] public required string Username { get; set; }
    [MaxLength(255)] public required string Email { get; set; }
    [MaxLength(255)] public required string HashedPassword { get; set; }

    // Auth-specific properties
    [MaxLength(255)] public string? RefreshToken { get; set; }

    [DisplayFormat(DataFormatString = "{0:d}")]
    public DateTime? RefreshTokenExpiry { get; set; }

    public DateTime? LastLogin { get; set; }
    public int FailedLoginAttempts { get; set; }
}
