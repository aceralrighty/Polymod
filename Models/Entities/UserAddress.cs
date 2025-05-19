using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TBD.Models.Entities;

[Table("Address")]
public class UserAddress : GenericEntity
{
    [Required] public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))] public User User { get; set; }

    [Required] [MaxLength(int.MaxValue)] public string? Address1 { get; set; }
    [MaxLength(int.MaxValue)] public string? Address2 { get; set; }
    [Required] [MaxLength(int.MaxValue)] public string? City { get; set; }
    [Required] [MaxLength(int.MaxValue)] public string? State { get; set; }
    [Required] [MaxLength(int.MaxValue)] public int? ZipCode { get; set; }
}