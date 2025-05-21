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

    [Required] [MaxLength(255)] public string? Address1 { get; set; }
    [MaxLength(255)] public string? Address2 { get; set; }
    [Required] [MaxLength(255)] public string City { get; set; }
    [Required] [MaxLength(255)] public string State { get; set; }

    [Required]
    [RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Invalid ZIP code")]
    public int ZipCode { get; set; }

    public UserAddress(Guid userId, User user, string? address1, string? address2, string? city, string? state,
        int? zipCode)
    {
        UserId = userId;
        User = user;
        Address1 = address1 ?? throw new ArgumentNullException(nameof(address1), "Address1 is required");
        Address2 = address2;
        City = city ?? throw new ArgumentNullException(nameof(city), "City is required");
        State = state ?? throw new ArgumentNullException(nameof(state), "State is required");
        ZipCode = zipCode ?? throw new ArgumentNullException(nameof(zipCode), "ZipCode is required");
    }

    internal UserAddress()
    {
    }
}