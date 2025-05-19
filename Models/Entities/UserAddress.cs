using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TBD.Models.Entities;

[Table("Address")]
public class UserAddress(
    Guid userId,
    User user,
    string? address1,
    string? address2,
    string? city,
    string? state,
    int? zipCode)
    : GenericEntity
{
    [Required] public Guid UserId { get; set; } = userId;

    [ForeignKey(nameof(UserId))] public User User { get; set; } = user;

    [Required] [MaxLength(int.MaxValue)] public string? Address1 { get; set; } = address1;
    [MaxLength(int.MaxValue)] public string? Address2 { get; set; } = address2;
    [Required] [MaxLength(int.MaxValue)] public string? City { get; set; } = city;
    [Required] [MaxLength(int.MaxValue)] public string? State { get; set; } = state;
    [Required] [MaxLength(int.MaxValue)] public int? ZipCode { get; set; } = zipCode;
}