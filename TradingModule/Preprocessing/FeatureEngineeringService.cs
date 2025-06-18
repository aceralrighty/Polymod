using TBD.TradingModule.Core.Entities;

namespace TBD.TradingModule.Preprocessing;

public class FeatureEngineeringService
{
    public record FeatureSet(StockFeatureVector Vector, LabelGenerator Labels);

    public List<FeatureSet> GenerateFeatureSets(List<RawMarketData> rawData)
    {
        if (rawData == null || rawData.Count < 51)
        {
            throw new ArgumentException("Insufficient data for feature generation. Need at least 51 data points.");
        }

        var sorted = rawData.OrderBy(r => r.Date).ToList();
        var output = new List<FeatureSet>();
        if (sorted.Any(d => d.Close <= 0 || d.Volume < 0))
        {
            throw new ArgumentException("Invalid market data detected (negative prices or volumes).");
        }

        for (var i = 50; i < sorted.Count - 1; i++)
        {
            var current = sorted[i];
            var next = sorted[i + 1];

            var closes = sorted.Skip(i - 20).Take(20).Select(r => (double)r.Close).ToList();
            var volumes = sorted.Skip(i - 20).Take(20).Select(r => (double)r.Volume).ToList();

            var nextDayReturn = (next.Close - current.Close) / current.Close;
            var nextDayVol = (next.High - next.Low) / next.Close;

            var vector = new StockFeatureVector
            {
                Symbol = current.Symbol,
                Date = current.Date,
                PriceReturn1Day = CalcReturn(sorted[i - 1], current),
                PriceReturn5Day = CalcReturn(sorted[i - 5], current),
                PriceReturn20Day = CalcReturn(sorted[i - 20], current),
                MA5Ratio = current.Close / (decimal)Ma(sorted, i, 5),
                MA10Ratio = (float)(current.Close / (decimal)Ma(sorted, i, 10)),
                MA20Ratio = (float)(current.Close / (decimal)Ma(sorted, i, 20)),
                MA50Ratio = (float)(current.Close / (decimal)Ma(sorted, i, 50)),
                RSI = CalcRsi(sorted, i),
                MACD = CalcMacd(sorted, i).macd,
                MACDSignal = CalcMacd(sorted, i).signal,
                BollingerPosition = CalcBollingerPosition(closes, (float)current.Close),
                VolumeRatio20Day = current.Volume / (float)volumes.Average(),
                VolumeRatioMA = current.Volume / (float)sorted.Skip(i - 10).Take(10).Average(v => v.Volume),
                Volatility20Day = CalcStdDev(closes),
                HighLowRatio = (float)((current.High - current.Low) / current.Close),
                MarketBeta = 1f,
                SectorPerformance = 0f,
                NextDayReturn = (float?)nextDayReturn,
                NextDayVolatility = (float?)nextDayVol
            };

            var label = new LabelGenerator
            {
                NextDayReturn = (float)nextDayReturn,
                VolatilityScore = (float)nextDayVol,
                SharpeRatio = (float)(nextDayReturn / (nextDayVol + (decimal)1e-6f)),
                IsHighReturnLowRisk = nextDayReturn > (decimal)0.01f && nextDayVol < (decimal)0.015f
            };

            output.Add(new FeatureSet(vector, label));
        }

        return output;
    }

    private static float CalcReturn(RawMarketData prev, RawMarketData current)
        => (float)((current.Close - prev.Close) / prev.Close);

    private static float Ma(List<RawMarketData> data, int index, int period)
        => (float)data.Skip(index - period).Take(period).Average(d => d.Close);

    private static float CalcRsi(List<RawMarketData> data, int index, int period = 14)
    {
        var gains = 0f;
        var losses = 0f;

        for (var i = index - period + 1; i <= index; i++)
        {
            var delta = data[i].Close - data[i - 1].Close;
            if (delta >= 0) gains += (float)delta;
            else losses -= (float)delta;
        }

        if (gains + losses == 0) return 50;
        var rs = gains / losses;
        return 100 - (100 / (1 + rs));
    }

    private static (float macd, float signal) CalcMacd(List<RawMarketData> data, int index)
    {
        var ema12 = CalcEma(data, index, 12);
        var ema26 = CalcEma(data, index, 26);
        var macdLine = ema12 - ema26;

        var signalLine = CalcEmaFromArray(index, 9, x => CalcEma(data, x, 12) - CalcEma(data, x, 26));
        return (macdLine, signalLine);
    }

    private static float CalcEma(List<RawMarketData> data, int index, int period)
    {
        var k = 2f / (period + 1);
        var ema = data[index - period].Close;

        for (var i = index - period + 1; i <= index; i++)
        {
            ema = data[i].Close * (decimal)k + ema * (decimal)(1 - k);
        }

        return (float)ema;
    }

    private static float CalcEmaFromArray(int index, int period, Func<int, float> extractor)
    {
        var k = 2f / (period + 1);
        var ema = extractor(index - period);

        for (var i = index - period + 1; i <= index; i++)
        {
            var value = extractor(i);
            ema = value * k + ema * (1 - k);
        }

        return ema;
    }

    private static float CalcStdDev(List<double> values)
    {
        var avg = values.Average();
        return (float)Math.Sqrt(values.Average(v => Math.Pow(v - avg, 2)));
    }

    private static float CalcBollingerPosition(List<double> closes, float currentClose)
    {
        var mean = closes.Average();
        var stdDev = CalcStdDev(closes);
        var upper = mean + 2 * stdDev;
        var lower = mean - 2 * stdDev;

        if (upper == lower) return 0.5f;
        return (currentClose - (float)lower) / ((float)upper - (float)lower);
    }
}
