using Microsoft.ML.Data;

namespace TBD.RecommendationModule.Models;

public class ServiceRating
{
    [LoadColumn(0)] public float UserId { get; set; }

    [LoadColumn(1)] public float ServiceId { get; set; }

    [LoadColumn(2)] public float Label { get; set; }
}
