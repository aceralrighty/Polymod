using System.ComponentModel.DataAnnotations;
using PolyMod.Shared.GenericDBProperties;
using PolyMod.UserModule.Models;

namespace PolyMod.AddressModule.Models;

public class UserAddress
    : BaseTableProperties
{
    public Guid UserId { get; set; }


    public User? User { get; set; }

    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }

    [RegularExpression(@"^[0-9]{5}(?:-[0-9]{4})?$", ErrorMessage = "Invalid ZIP code format. Use 12345 or 12345-6789.")]
    public string? ZipCode { get; set; }

    public UserAddress(Guid userId, User? user, string? address1, string? address2, string city,
        string state, string? zipCode)
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
