using TBD.TradingModule.Infrastructure.MarketData;
using TBD.TradingModule.ML;
using TBD.TradingModule.Preprocessing;

namespace TBD.TradingModule.Orchestration;

public class TrainingOrchestrator(
    TradingDbContext context,
    MarketDataFetcher marketDataFetcher,
    FeatureEngineeringService feature,
    StockPredictionEngine engine)
{
    private StockPredictionEngine _engine = engine;

    public async Task TrainAndPredictForSymbolAsync(string symbol, DateTime start, DateTime end)
    {
        var raw = await marketDataFetcher.GetHistoricalDataAsync(symbol, start, end);
        var sets = feature.GenerateFeatureSets(raw);
        await _engine.TrainAndPredictFromFeatureSetsAsync(sets, context);
    }

    public async Task BatchRunAsync(List<string> symbols, DateTime start, DateTime end)
    {
        foreach (var symbol in symbols)
        {
            Console.WriteLine($"Processing: {symbol}");
            await TrainAndPredictForSymbolAsync(symbol, start, end);
        }
    }
}
