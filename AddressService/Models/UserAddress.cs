using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TBD.UserModule.Models;

namespace TBD.AddressService.Models;

[Table("Address")]
public class UserAddress
    : GenericAddressEntity
{
    [Required] public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))] public User User { get; set; }

    [Required] [MaxLength(int.MaxValue)] public string? Address1 { get; set; }
    [MaxLength(int.MaxValue)] public string? Address2 { get; set; }
    [Required] [MaxLength(int.MaxValue)] public string? City { get; set; }
    [Required] [MaxLength(int.MaxValue)] public string? State { get; set; }
    [Required] public int? ZipCode { get; set; }
    
    public UserAddress(Guid userId, User user, string? address1, string? address2, string? city, string? state, int? zipCode)
    {
        UserId = userId;
        User = user;
        Address1 = address1;
        Address2 = address2;
        City = city;
        State = state;
        ZipCode = zipCode;
    }

    internal UserAddress() { }
}