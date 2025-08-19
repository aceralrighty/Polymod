using System.ComponentModel.DataAnnotations.Schema;
using PolyMod.Shared.GenericDBProperties;

namespace TBD.UserModule.Models;

public class User : BaseTableProperties
{
    [Column(TypeName = "varchar(255)")] public string? Username { get; set; }

    [Column(TypeName = "varchar(255)")] public string? Email { get; set; }

    [Column(TypeName = "varchar(255)")] public string? Password { get; set; }
}
