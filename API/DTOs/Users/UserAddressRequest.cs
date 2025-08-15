using System.ComponentModel.DataAnnotations;

namespace TBD.API.DTOs.Users;

public class UserAddressRequest
{
    public Guid Id { get; set; }

    [Required] public Guid UserId { get; set; }

    [Required] [MaxLength(int.MaxValue)] public string? Address1 { get; set; }
    [MaxLength(int.MaxValue)] public string? Address2 { get; set; }
    [Required] [MaxLength(int.MaxValue)] public string? City { get; set; }
    [Required] [MaxLength(int.MaxValue)] public string? State { get; set; }
    [Required] public string? ZipCode { get; set; }
}
