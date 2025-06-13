using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TBD.GenericDBProperties;
using TBD.ServiceModule.Models;
using TBD.UserModule.Models;

namespace TBD.RecommendationModule.Models;

public class UserRecommendation : BaseTableProperties
{
    [Required] public Guid UserId { get; set; }
    [Required] public Guid ServiceId { get; set; }

    public float Rating { get; set; } = 0f;

    [ForeignKey(nameof(UserId))] public User? User { get; set; }

    [ForeignKey(nameof(ServiceId))] public Service? Service { get; set; }

    public DateTime RecommendedAt { get; set; }
    public int ClickCount { get; set; } = 0;

    public ServiceRatingPrediction? ServiceRatingPrediction { get; set; } = new ServiceRatingPrediction();
}
