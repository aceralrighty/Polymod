using TBD.StockPredictionModule.Models;

namespace TBD.StockPredictionModule.PipelineOrchestrator;

public static class FeatureEngineering
{
    public static List<StockFeatureVector> GenerateFeatures(List<RawData> rawData)
    {
        var result = new List<StockFeatureVector>();
        var grouped = rawData.Where(r => r is { Close: > 0, Volume: > 0 }).GroupBy(r => r.Symbol);

        foreach (var group in grouped)
        {
            var ordered = group.OrderBy(r => DateTime.Parse(r.Date)).ToList();

            for (var i = 10; i < ordered.Count - 1; i++) // leave 1 for prediction target
            {
                var window5 = ordered.Skip(i - 5).Take(5).ToList();
                var window10 = ordered.Skip(i - 10).Take(10).ToList();

                var today = ordered[i];
                var tomorrow = ordered[i + 1];

                result.Add(new StockFeatureVector
                {
                    Open = today.Open,
                    High = today.High,
                    Low = today.Low,
                    Close = today.Close,
                    Volume = today.Volume,
                    MA5 = window5.Average(x => x.Close),
                    MA10 = window10.Average(x => x.Close),
                    Volatility5 =
                        (float)Math.Sqrt(window5.Average(x => Math.Pow(x.Close - window5.Average(w => w.Close), 2))),
                    Return1D = (today.Close - ordered[i - 1].Close) / ordered[i - 1].Close,
                    NextClose = tomorrow.Close // Supervised label
                });
            }
        }

        return result;
    }
}
