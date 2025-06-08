using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TBD.AuthModule.Models;
using TBD.GenericDBProperties;
using TBD.ScheduleModule.Models;

namespace TBD.UserModule.Models;

public class User : BaseTableProperties
{
    [Key] public new Guid Id { get; set; }

    [Column(TypeName = "varchar(255)")] public required string? Username { get; set; }

    [Column(TypeName = "varchar(255)")] public required string? Email { get; set; }

    [Column(TypeName = "varchar(255)")] public required string? Password { get; set; }

    public required Schedule? Schedule { get; init; } = new();
}
