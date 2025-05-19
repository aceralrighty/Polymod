using TBD.Models.Entities;

namespace TBD.Models.DTOs;

public class UserAddressRequest(Guid id,string address1, string address2, string city, string state, int zipCode)
{
    public Guid Id { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public int? ZipCode { get; set; }
}