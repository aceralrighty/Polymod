using Microsoft.EntityFrameworkCore;
using TBD.TradingModule.Core.Entities;
using TBD.TradingModule.Infrastructure.MarketData;

namespace TBD.TradingModule.Orchestration.Supporting;

public class DatabaseVerificationHelper(TradingDbContext dbContext, ILogger<DatabaseVerificationHelper> logger)
{
    public async Task VerifyDatabaseAsync()
    {
        try
        {
            logger.LogInformation("Verifying database connection...");

            // Test database connection
            var canConnect = await dbContext.Database.CanConnectAsync();
            logger.LogInformation("Database connection: {Status}", canConnect ? "SUCCESS" : "FAILED");

            if (!canConnect)
            {
                throw new InvalidOperationException("Cannot connect to database");
            }

            // Check if tables exist
            var rawDataCount = await dbContext.RawData.CountAsync();
            var featuresCount = await dbContext.StockFeatures.CountAsync();
            var predictionsCount = await dbContext.Predictions.CountAsync();
            var apiLogsCount = await dbContext.ApiRequestLogs.CountAsync();

            logger.LogInformation("Database record counts:");
            logger.LogInformation("  Raw Market Data: {Count}", rawDataCount);
            logger.LogInformation("  Stock Features: {Count}", featuresCount);
            logger.LogInformation("  Predictions: {Count}", predictionsCount);
            logger.LogInformation("  API Request Logs: {Count}", apiLogsCount);

            // Test insert
            var testRecord = new RawMarketData
            {
                Symbol = "TEST",
                Date = DateTime.UtcNow.Date,
                Open = 100m,
                High = 105m,
                Low = 95m,
                Close = 102m,
                AdjustedClose = 102m,
                Volume = 1000
            };

            dbContext.RawData.Add(testRecord);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Test record inserted successfully");

            // Clean up test record
            dbContext.RawData.Remove(testRecord);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Test record removed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database verification failed");
            throw;
        }
    }
}
