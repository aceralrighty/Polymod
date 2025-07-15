using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using TBD.MetricsModule.ModuleHealthCheck.BaseHealthCheck.DBLevel;
using TBD.MetricsModule.ModuleHealthCheck.BaseHealthCheck.ModuleLevel;
using TBD.StockPredictionModule.Context;
using TBD.StockPredictionModule.Models;
using TBD.StockPredictionModule.PipelineOrchestrator.Interface;

namespace TBD.MetricsModule.ModuleHealthCheck.ModuleChecks;

public class StockPredictionModuleHealthCheck(IServiceProvider serviceProvider, ILogger<BaseModuleHealthCheck> logger)
    : DatabaseModuleHealthCheck<StockDbContext>(serviceProvider, logger)
{
    private readonly ILogger<BaseModuleHealthCheck> _logger1 = logger;
    public override string ModuleName => "stockPrediction";

    protected override async Task<Dictionary<string, object>> GetAdditionalHealthDataAsync(StockDbContext dbContext, CancellationToken cancellationToken)
    {
        var stockService = ServiceProvider.GetService<IStockPredictionPipeline>();
        var healthData = new Dictionary<string, object>();

        try
        {
            // Database health metrics
            var recordCount = await dbContext.StockPredictions.CountAsync(cancellationToken);
            healthData["recordCount"] = recordCount;

            // Check for recent data (last 30 days)
            var recentDataCount = await dbContext.StockPredictions
                .Where(sp => sp.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                .CountAsync(cancellationToken);
            healthData["recentDataCount"] = recentDataCount;

            // Check data quality - count unique symbols
            var uniqueSymbols = await dbContext.StockPredictions
                .Select(sp => sp.Symbol)
                .Distinct()
                .CountAsync(cancellationToken);
            healthData["uniqueSymbols"] = uniqueSymbols;

            // Get last updated timestamp
            var lastUpdated = await dbContext.StockPredictions
                .OrderByDescending(sp => sp.CreatedAt)
                .Select(sp => sp.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
            healthData["lastDataUpdate"] = lastUpdated;

            // Service availability check
            var serviceAvailable = stockService != null;
            healthData["serviceAvailable"] = serviceAvailable;

            // ML Model health checks
            if (serviceAvailable)
            {
                try
                {
                    // Get sample data for accuracy check
                    var sampleData = await GetSampleDataForAccuracyCheck(dbContext, cancellationToken);

                    if (sampleData.Count != 0)
                    {
                        // Test model prediction capability
                        var accuracy = await stockService?.PerformQuickAccuracyCheck(sampleData)!;
                        healthData["accuracy"] = accuracy ?? 0.0;
                        healthData["modelLoaded"] = accuracy.HasValue;
                    }
                    else
                    {
                        healthData["accuracy"] = 0.0;
                        healthData["modelLoaded"] = false;
                        healthData["warning"] = "No sample data available for accuracy check";
                    }

                    // Memory usage check
                    var memoryUsage = GetModelMemoryUsage();
                    healthData["memoryUsageMB"] = memoryUsage;

                    // Performance benchmark
                    var avgPredictionTime = await MeasureAveragePredictionTime(stockService ?? throw new InvalidOperationException(), sampleData);
                    healthData["avgPredictionTimeMs"] = avgPredictionTime;
                }
                catch (Exception ex)
                {
                    _logger1.LogWarning(ex, "Error checking model health for {ModuleName}", ModuleName);
                    healthData["modelLoaded"] = false;
                    healthData["accuracy"] = 0.0;
                    healthData["modelError"] = ex.Message;
                }
            }
            else
            {
                healthData["modelLoaded"] = false;
                healthData["accuracy"] = 0.0;
                healthData["serviceAvailable"] = false;
            }

            // Overall health assessment
            healthData["dataQualityScore"] = CalculateDataQualityScore(recordCount, recentDataCount, uniqueSymbols, lastUpdated);
            healthData["overallHealthScore"] = CalculateOverallHealthScore(healthData);

        }
        catch (Exception ex)
        {
            _logger1.LogError(ex, "Error performing health check for {ModuleName}", ModuleName);
            healthData["error"] = ex.Message;
            healthData["healthy"] = false;
        }

        return healthData;
    }

    protected override string GetHealthyStatus(Dictionary<string, object> additionalData)
    {
        if (additionalData.ContainsKey("error"))
        {
            return $"❌ Error: {additionalData["error"]}";
        }

        var serviceAvailable = (bool)additionalData["serviceAvailable"];
        var modelLoaded = (bool)additionalData["modelLoaded"];
        var accuracy = (double)additionalData["accuracy"];
        var recordCount = (int)additionalData["recordCount"];
        var overallScore = (double)additionalData["overallHealthScore"];

        // Determine status based on multiple factors
        if (!serviceAvailable)
        {
            return "❌ Service not available";
        }

        if (!modelLoaded)
        {
            return "⚠️ Model not loaded or prediction failed";
        }

        if (accuracy < 50.0)
        {
            return $"❌ Model accuracy critically low: {accuracy:F2}%";
        }

        if (accuracy < 90.0)
        {
            return $"⚠️ Model accuracy low: {accuracy:F2}%";
        }

        if (recordCount < 100000)
        {
            return $"⚠️ Low data volume: {recordCount:N0} records";
        }

        if (overallScore >= 0.9)
        {
            return $"✅ Excellent - Model: {accuracy:F2}% accuracy, {recordCount:N0} records";
        }
        else if (overallScore >= 0.7)
        {
            return $"🟡 Good - Model: {accuracy:F2}% accuracy, {recordCount:N0} records";
        }
        else
        {
            return $"⚠️ Needs attention - Model: {accuracy:F2}% accuracy, {recordCount:N0} records";
        }
    }

    protected override string GetDescription()
    {
        return "ML.NET stock prediction with historical data analysis and real-time model monitoring";
    }

    protected override string[] GetEndpoints()
    {
        return [
            "/api/stock/predict/{symbol}",
            "/api/stock/historical/{symbol}",
            "/api/stock/batch-predict",
            "/api/stock/model-stats",
            "/api/stock/health"
        ];
    }

    private async Task<Dictionary<string, List<RawData>>> GetSampleDataForAccuracyCheck(StockDbContext dbContext, CancellationToken cancellationToken)
    {
        try
        {
            // Get a sample of recent data grouped by symbol for accuracy testing
            var sampleData = await dbContext.StockPredictions
                .Where(sp => sp.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                .OrderByDescending(sp => sp.CreatedAt)
                .Take(100) // Limit to avoid performance issues
                .Select(sp => new RawData
                {
                    Symbol = sp.Symbol,
                    Date = sp.CreatedAt.ToString("yyyy-MM-dd"),
                })
                .ToListAsync(cancellationToken);

            return sampleData.GroupBy(d => d.Symbol)
                           .ToDictionary(g => g.Key, g => g.ToList());
        }
        catch (Exception ex)
        {
            _logger1.LogWarning(ex, "Error getting sample data for accuracy check");
            return new Dictionary<string, List<RawData>>();
        }
    }

    private double GetModelMemoryUsage()
    {
        var beforeGc = GC.GetTotalMemory(false);
        var afterGc = GC.GetTotalMemory(true);
        return Math.Round((afterGc / 1024.0 / 1024.0), 2); // MB
    }

    private async Task<double> MeasureAveragePredictionTime(IStockPredictionPipeline stockService, Dictionary<string, List<RawData>> sampleData)
    {
        try
        {
            if (!sampleData.Any()) return -1;

            var times = new List<double>();
            var testCount = Math.Min(3, sampleData.Count); // Test with up to 3 symbols
            var testData = sampleData.Take(testCount).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            for (int i = 0; i < testCount; i++)
            {
                var stopwatch = Stopwatch.StartNew();

                // Perform accuracy check as a way to test prediction performance
                await stockService.PerformQuickAccuracyCheck(testData);

                stopwatch.Stop();
                times.Add(stopwatch.ElapsedMilliseconds);
            }

            return times.Any() ? times.Average() : -1;
        }
        catch (Exception ex)
        {
            _logger1.LogWarning(ex, "Error measuring prediction time");
            return -1;
        }
    }

    private double CalculateDataQualityScore(int recordCount, int recentDataCount, int uniqueSymbols, DateTime? lastUpdated)
    {
        double score = 0.0;

        score += recordCount switch
        {
            // Volume score (40%)
            > 500000 => 0.4,
            > 100000 => 0.3,
            > 10000 => 0.2,
            _ => 0.1
        };

        // Freshness score (30%)
        if (lastUpdated.HasValue)
        {
            var daysSinceUpdate = (DateTime.UtcNow - lastUpdated.Value).TotalDays;
            switch (daysSinceUpdate)
            {
                case <= 1:
                    score += 0.3;
                    break;
                case <= 7:
                    score += 0.2;
                    break;
                case <= 30:
                    score += 0.1;
                    break;
            }
        }

        score += uniqueSymbols switch
        {
            // Diversity score (20%)
            > 100 => 0.2,
            > 50 => 0.15,
            > 20 => 0.1,
            _ => 0.05
        };

        switch (recentDataCount)
        {
            // Recent activity score (10%)
            case > 1000:
                score += 0.1;
                break;
            case > 100:
                score += 0.05;
                break;
        }

        return Math.Min(1.0, score);
    }

    private double CalculateOverallHealthScore(Dictionary<string, object> healthData)
    {
        double score = 0.0;

        // Service availability (20%)
        if ((bool)healthData["serviceAvailable"])
        {
            score += 0.2;
        }

        // Model health (40%)
        if ((bool)healthData["modelLoaded"])
        {
            var accuracy = (double)healthData["accuracy"];
            switch (accuracy)
            {
                case >= 95:
                    score += 0.4;
                    break;
                case >= 90:
                    score += 0.3;
                    break;
                case >= 80:
                    score += 0.2;
                    break;
                case >= 50:
                    score += 0.1;
                    break;
            }
        }

        // Data quality (30%)
        if (healthData.ContainsKey("dataQualityScore"))
        {
            score += (double)healthData["dataQualityScore"] * 0.3;
        }

        // Performance (10%)
        if (!healthData.TryGetValue("avgPredictionTimeMs", out var value))
        {
            return Math.Min(1.0, score);
        }

        var avgTime = (double)value;
        switch (avgTime)
        {
            case > 0 and < 1000:
                score += 0.1;
                break;
            case < 5000:
                score += 0.05;
                break;
        }

        return Math.Min(1.0, score);
    }
}
