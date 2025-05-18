using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TBD.Models;

[Table("Users")]
public class User : GenericEntity
{
    [Column(TypeName = "varchar(255)")]
    [Required]
    public string? Username { get; set; }

    [Column(TypeName = "varchar(255)")]
    [Required]
    public string? Email { get; set; }
}