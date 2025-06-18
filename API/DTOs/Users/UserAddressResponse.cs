namespace TBD.API.DTOs.Users;

public class UserAddressResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public int ZipCode { get; set; }
}
