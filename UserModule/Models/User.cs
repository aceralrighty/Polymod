using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TBD.ScheduleModule.Models;

namespace TBD.UserModule.Models;

[Table("Users")]
public class User: GenericUserEntity
{
    [Key] public new Guid Id { get; set; }

    [Column(TypeName = "varchar(255)")]
    [Required]
    public string? Username { get; set; }

    [Column(TypeName = "varchar(255)")]
    [Required]
    public string? Email { get; set; }

    [Required] public Schedule? Schedule { get; init; }
    
}