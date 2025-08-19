using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TBD.API.DTOs.Users;

public class UserDto
{
    public UserDto() { }

    public UserDto(string username, string email, string password)
    {
        Username = username;
        Email = email;
        Password = password;
    }

    public UserDto(Guid id, string username, string email, string password)
    {
        Id = id;
        Username = username;
        Email = email;
        Password = password;
    }

    public UserDto(Guid id, string email)
    {
        Id = id;
        Email = email;
    }

    [Key] public Guid Id { get; set; }

    [Column(TypeName = "varchar(255)")]
    [Required]
    public string? Username { get; set; }

    [Column(TypeName = "varchar(255)")]
    [Required]
    public string Email { get; set; }

    [Column(TypeName = "varchar(255)")]
    [Required]
    public string? Password { get; set; }
}
