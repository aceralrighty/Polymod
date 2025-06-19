namespace TBD.RecommendationModule.Models.Recommendations;

public class RecommendationAnalytics
{
    public Guid UserId { get; set; }
    public int TotalRecommendations { get; set; }
    public int ViewedRecommendations { get; set; }
    public int ClickedRecommendations { get; set; }
    public double ViewRate { get; set; }
    public double ClickThroughRate { get; set; }
    public double ConversionRate { get; set; }
}
