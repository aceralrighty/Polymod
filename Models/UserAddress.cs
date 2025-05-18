using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TBD.Models;

[Table("Address")]
public class UserAddress : GenericEntity
{
    [Required] public Guid UserId { get; set; }
    
    [ForeignKey(nameof(UserId))] public User User { get; set; }

    [Required] public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    [Required] public string? City { get; set; }
    [Required] public string? State { get; set; }
    [Required] public int? ZipCode { get; set; }
}