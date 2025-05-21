using System.ComponentModel.DataAnnotations;

namespace TBD.Shared.DTOs;

public class UserAddressRequest(Guid id, string? address1, string? address2, string? city, string? state, int? zipCode)
{
    public Guid Id { get; set; } = id;
    [Required] [MaxLength(int.MaxValue)] public string? Address1 { get; set; } = address1;
    [MaxLength(int.MaxValue)] public string? Address2 { get; set; } = address2;
    [Required] [MaxLength(int.MaxValue)] public string? City { get; set; } = city;
    [Required] [MaxLength(int.MaxValue)] public string? State { get; set; } = state;
    [Required] public int? ZipCode { get; set; } = zipCode;
}