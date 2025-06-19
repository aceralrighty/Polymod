using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TBD.GenericDBProperties;
using TBD.ServiceModule.Models;
using TBD.UserModule.Models;

namespace TBD.RecommendationModule.Models.Recommendations;

public class RecommendationOutput : BaseTableProperties
{
    [Required] public Guid UserId { get; init; }
    [Required] public Guid ServiceId { get; init; }

    /// <summary>
    /// The ML model's confidence score for this recommendation
    /// </summary>
    public float Score { get; init; }

    /// <summary>
    /// Ranking position in the recommendation list (1-based)
    /// </summary>
    public int Rank { get; init; }

    /// <summary>
    /// Which recommendation strategy was used
    /// </summary>
    [MaxLength(50)]
    public string Strategy { get; init; } = "MatrixFactorization";

    /// <summary>
    /// Context when the recommendation was generated (morning, evening, weekend, etc.)
    /// </summary>
    [MaxLength(100)]
    public string? Context { get; init; }

    /// <summary>
    /// Batch identifier to group recommendations generated together
    /// </summary>
    public Guid BatchId { get; init; }

    /// <summary>
    /// When this recommendation was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// Has the user interacted with this recommendation?
    /// </summary>
    public bool HasBeenViewed { get; set; }

    /// <summary>
    /// Has the user clicked on this recommendation?
    /// </summary>
    public bool HasBeenClicked { get; set; }

    /// <summary>
    /// When the user first viewed this recommendation
    /// </summary>
    public DateTime? ViewedAt { get; set; }

    /// <summary>
    /// When the user clicked on this recommendation
    /// </summary>
    public DateTime? ClickedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UserId))] public User? User { get; init; }
    [ForeignKey(nameof(ServiceId))] public Service? Service { get; init; }
}
