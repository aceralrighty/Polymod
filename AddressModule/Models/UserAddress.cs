using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TBD.GenericDBProperties;
using TBD.UserModule.Models;

namespace TBD.AddressModule.Models;

public class UserAddress
    : BaseTableProperties
{
    [Required] public Guid UserId { get; set; }

    [Required]
    [ForeignKey(nameof(UserId))]
    public User User { get; set; }

    [Required] [MaxLength(255)] public string Address1 { get; set; }
    [MaxLength(255)] public string? Address2 { get; set; }
    [Required] [MaxLength(255)] public string? City { get; set; }
    [Required] [MaxLength(255)] public string? State { get; set; }

    [Required]
    [RegularExpression(@"^[0-9]{5}(?:-[0-9]{4})?$", ErrorMessage = "Invalid ZIP code format. Use 12345 or 12345-6789.")]
    [MaxLength(10)]
    public string ZipCode { get; set; }

    public UserAddress(Guid userId, User user, string address1, string? address2, string city,
        string state, string zipCode)
    {
        UserId = userId;
        User = user;
        Address1 = address1;
        Address2 = address2;
        City = city;
        State = state;
        ZipCode = zipCode;
    }


    public UserAddress()
    {
    }
}
