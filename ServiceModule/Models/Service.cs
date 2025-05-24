using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TBD.ServiceModule.Models;

[Table("Services")]
public class Service
{
    [Key] public Guid Id { get; set; }
    [MaxLength(255)] public string Title { get; set; } = null!;
    [MaxLength(255)] public string Description { get; set; } = null!;

    public decimal Price { get; set; }
    public int DurationInMinutes { get; set; }
    public Guid ProviderId { get; set; }
    private decimal TotalPrice => Price * DurationInMinutes;

    public string FormattedPrice => TotalPrice.ToString("C");
}