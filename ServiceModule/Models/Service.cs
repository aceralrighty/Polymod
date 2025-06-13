using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TBD.GenericDBProperties;

namespace TBD.ServiceModule.Models;

public class Service : BaseTableProperties
{
    public Guid ProviderId { get; init; }
    [MaxLength(255)] public string Title { get; init; } = null!;
    [MaxLength(255)] public string Description { get; init; } = null!;


    public decimal Price { get; init; }
    public int DurationInMinutes { get; init; }

    public decimal TotalPrice { get; init; }

    [NotMapped] public string FormattedPrice => TotalPrice.ToString("C");
}
