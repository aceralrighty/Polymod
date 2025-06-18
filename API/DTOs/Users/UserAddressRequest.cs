using System.ComponentModel.DataAnnotations;

namespace TBD.API.DTOs.Users;

public class UserAddressRequest
{
    public UserAddressRequest()
    {
    }

    public UserAddressRequest(Guid id, Guid userId, string? address1, string? address2, string? city, string? state,
        int zipCode)
    {
        Id = id;
        UserId = userId;
        Address1 = address1;
        Address2 = address2;
        City = city;
        State = state;
        ZipCode = zipCode;
    }

    public Guid Id { get; set; }

    [Required] public Guid UserId { get; set; }

    [Required] [MaxLength(int.MaxValue)] public string? Address1 { get; set; }
    [MaxLength(int.MaxValue)] public string? Address2 { get; set; }
    [Required] [MaxLength(int.MaxValue)] public string? City { get; set; }
    [Required] [MaxLength(int.MaxValue)] public string? State { get; set; }
    [Required] public int ZipCode { get; set; }
}
