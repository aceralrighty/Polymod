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

    [ForeignKey(nameof(UserId))]
    [Required]
    public User? User { get; set; }

    [ForeignKey(nameof(ServiceId))]
    [Required]
    public Service? Service { get; set; }

    public DateTime RecommendedAt { get; set; }
    public int ClickCount { get; set; } = 0;
}
