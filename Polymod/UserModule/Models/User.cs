using System.ComponentModel.DataAnnotations.Schema;
using PolyMod.Shared.GenericDBProperties;
using BaseTableProperties = PolyMod.StockPredictionService.Shared.GenericDBProperties.BaseTableProperties;

namespace PolyMod.UserModule.Models;

public class User : BaseTableProperties
{
    [Column(TypeName = "varchar(255)")] public string? Username { get; set; }

    [Column(TypeName = "varchar(255)")] public string? Email { get; set; }

    [Column(TypeName = "varchar(255)")] public string? Password { get; set; }
}
