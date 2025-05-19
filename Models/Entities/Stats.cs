using System.ComponentModel.DataAnnotations.Schema;

namespace TBD.Models.Entities;

[Table("Stats")]
public class Stats : GenericEntity
{
    [Column(nameof(TotalUsers))] public int TotalUsers { get; set; }
    [Column(nameof(Bike))] public double Bike { get; set; }
    [Column(nameof(Run))] public double Run { get; set; }
    [Column(nameof(Walk))] public double Walk { get; set; }
}