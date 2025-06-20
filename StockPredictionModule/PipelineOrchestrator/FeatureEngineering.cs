using TBD.StockPredictionModule.Models;
using TBD.StockPredictionModule.Models.Stocks;

namespace TBD.StockPredictionModule.PipelineOrchestrator;

public static class FeatureEngineering
{
    public static List<StockFeatureVector> GenerateFeatures(List<RawData> rawData)
    {
        var grouped = rawData.Where(r => r is { Close: > 0, Volume: > 0 }).GroupBy(r => r.Symbol);

        // Pre-calculate total size to avoid List reallocations
        var enumerable = grouped as IGrouping<string, RawData>[] ?? grouped.ToArray();
        var estimatedSize = enumerable.Sum(g => Math.Max(0, g.Count() - 11));
        var result = new List<StockFeatureVector>(estimatedSize);

        foreach (var group in enumerable)
        {
            var ordered = group.OrderBy(r => DateTime.Parse(r.Date)).ToList();

            // Pre-allocate arrays for moving calculations to avoid repeated LINQ allocations
            var closePrices = new float[ordered.Count];
            for (var idx = 0; idx < ordered.Count; idx++)
            {
                closePrices[idx] = ordered[idx].Close;
            }

            for (var i = 10; i < ordered.Count - 1; i++)
            {
                var today = ordered[i];
                var tomorrow = ordered[i + 1];

                // Calculate MA5 without creating temporary collections
                var ma5Sum = 0f;
                for (var j = i - 4; j <= i; j++)
                {
                    ma5Sum += closePrices[j];
                }
                var ma5 = ma5Sum / 5f;

                // Calculate MA10 without creating temporary collections
                var ma10Sum = 0f;
                for (var j = i - 9; j <= i; j++)
                {
                    ma10Sum += closePrices[j];
                }
                var ma10 = ma10Sum / 10f;

                // Calculate volatility without temporary collections
                var variance = 0f;
                for (var j = i - 4; j <= i; j++)
                {
                    var diff = closePrices[j] - ma5;
                    variance += diff * diff;
                }
                var volatility5 = (float)Math.Sqrt(variance / 5f);

                result.Add(new StockFeatureVector
                {
                    Open = today.Open,
                    High = today.High,
                    Low = today.Low,
                    Close = today.Close,
                    Volume = today.Volume,
                    MA5 = ma5,
                    MA10 = ma10,
                    Volatility5 = volatility5,
                    Return1D = (today.Close - ordered[i - 1].Close) / ordered[i - 1].Close,
                    NextClose = tomorrow.Close
                });
            }
        }

        return result;
    }
}
