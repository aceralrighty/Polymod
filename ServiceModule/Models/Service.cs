using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TBD.ServiceModule.Models;

[Table("Services")]
public class Service
{
    [Key] public Guid Id { get; set; }
    [MaxLength(255)] public string Title { get; init; } = null!;
    [MaxLength(255)] public string Description { get; init; } = null!;

    public decimal Price { get; init; }
    public int DurationInMinutes { get; init; }
    public Guid ProviderId { get; init; }

    public decimal TotalPrice { get; init; }

    [NotMapped] public string FormattedPrice => TotalPrice.ToString("C");
}