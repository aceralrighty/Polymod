using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TBD.API.DTOs;

public class UserDto
{
    [Key] public Guid Id { get; set; }

    [Column(TypeName = "varchar(255)")]
    [Required]
    public string? Username { get; set; }

    [Column(TypeName = "varchar(255)")]
    [Required]
    public string? Email { get; set; }

    [Column(TypeName = "varchar(255)")]
    [Required]
    public string? Password { get; set; }
}
