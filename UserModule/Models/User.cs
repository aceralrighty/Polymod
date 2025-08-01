using System.ComponentModel.DataAnnotations.Schema;
using TBD.ScheduleModule.Models;
using TBD.Shared.GenericDBProperties;

namespace TBD.UserModule.Models;

public class User : BaseTableProperties
{
    [Column(TypeName = "varchar(255)")] public required string? Username { get; set; }

    [Column(TypeName = "varchar(255)")] public required string? Email { get; set; }

    [Column(TypeName = "varchar(255)")] public required string? Password { get; set; }

}
