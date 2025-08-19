using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PolyMod.RecommendationModule.Exceptions;
using PolyMod.Shared.GenericDBProperties;
using PolyMod.ServiceModule.Models;
using PolyMod.UserModule.Models;

namespace PolyMod.RecommendationModule.Models.Recommendations;

public class UserRecommendation : BaseTableProperties
{
    [Required]
    [GuidNotEmpty(ErrorMessage = "UserId is Required")]
    public Guid UserId { get; set; }

    [Required]
    [GuidNotEmpty(ErrorMessage = "ServiceId is required")]
    public Guid ServiceId { get; set; }

    public float Rating { get; set; }

    [ForeignKey(nameof(UserId))] public User? User { get; set; }

    [ForeignKey(nameof(ServiceId))] public Service? Service { get; set; }

    public DateTime RecommendedAt { get; set; }
    public int ClickCount { get; set; }
}
