using System.Data;
using System.Diagnostics;
using Dapper;
using Microsoft.EntityFrameworkCore;
using TBD.RecommendationModule.Data;
using TBD.RecommendationModule.Models.Recommendations;
using TBD.RecommendationModule.Repositories.Interfaces;
using TBD.Shared.Repositories;
using TBD.Shared.Repositories.Configuration;

namespace TBD.RecommendationModule.Repositories;

internal class RecommendationRepository(RecommendationDbContext context)
    : GenericRepository<UserRecommendation>(context), IRecommendationRepository
{
    public override async Task AddAsync(UserRecommendation userRecommendation)
    {
        await context.UserRecommendations.AddAsync(userRecommendation);
        await SaveChangesAsync();
    }

    public async Task<IEnumerable<UserRecommendation>> GetByUserIdAsync(Guid userId)
    {
        return await context.UserRecommendations.Where(u => u.UserId == userId).OrderByDescending(r => r.RecommendedAt)
            .ToListAsync();
    }

    public async Task<UserRecommendation?> GetLatestByUserAndServiceAsync(Guid userId, Guid serviceId)
    {
        return await context.UserRecommendations
            .Where(r => r.UserId == userId && r.ServiceId == serviceId)
            .OrderByDescending(r => r.RecommendedAt)
            .FirstOrDefaultAsync();
    }

    public async Task SaveChangesAsync()
    {
        await context.SaveChangesAsync();
    }

    // OPTIMIZED: High-performance version using raw SQL with Dapper
    public async Task<IEnumerable<UserRecommendation>> GetAllWithRatingsAsync()
    {
        var dbConnection = Context.Database.GetDbConnection();
        if (dbConnection.State != ConnectionState.Open)
            await dbConnection.OpenAsync();

        var tableName = GetTableName();
        var sql = $@"
            SELECT * FROM {tableName} WITH (NOLOCK)
            WHERE Rating > 0
            ORDER BY RecommendedAt DESC";

        Console.WriteLine("📊 Executing optimized GetAllWithRatingsAsync query...");
        var stopwatch = Stopwatch.StartNew();

        var result = await dbConnection.QueryAsync<UserRecommendation>(sql);
        var recommendations = result.ToList();

        stopwatch.Stop();
        Console.WriteLine(
            $"✅ Retrieved {recommendations.Count:N0} recommendations with ratings in {stopwatch.ElapsedMilliseconds}ms");

        return recommendations;
    }

    // OPTIMIZED: Chunked version for extremely large datasets
    public async Task<IEnumerable<UserRecommendation>> GetAllWithRatingsChunkedAsync(int chunkSize = 10000)
    {
        var dbConnection = Context.Database.GetDbConnection();
        if (dbConnection.State != ConnectionState.Open)
            await dbConnection.OpenAsync();

        var tableName = GetTableName();
        var allResults = new List<UserRecommendation>();
        var offset = 0;
        var hasMoreData = true;

        // Get the total count first
        var countSql = $"SELECT COUNT(*) FROM {tableName} WHERE Rating > 0";
        var totalCount = await dbConnection.QuerySingleAsync<int>(countSql);

        Console.WriteLine($"📊 Total recommendations with ratings to fetch: {totalCount:N0}");

        while (hasMoreData)
        {
            var sql = $@"
                SELECT * FROM {tableName} WITH (NOLOCK)
                WHERE Rating > 0
                ORDER BY RecommendedAt DESC
                OFFSET @Offset ROWS
                FETCH NEXT @ChunkSize ROWS ONLY";

            var chunk = await dbConnection.QueryAsync<UserRecommendation>(sql,
                new { Offset = offset, ChunkSize = chunkSize });
            var chunkList = chunk.ToList();

            if (chunkList.Count == 0)
            {
                hasMoreData = false;
            }
            else
            {
                allResults.AddRange(chunkList);
                offset += chunkSize;

                Console.WriteLine(
                    $"📈 Fetched {allResults.Count:N0}/{totalCount:N0} recommendations ({(double)allResults.Count / totalCount * 100:F1}%)");
            }
        }

        return allResults;
    }

    // OPTIMIZED: Streaming version for memory-efficient processing
    public async IAsyncEnumerable<UserRecommendation> GetAllWithRatingsStreamingAsync(int bufferSize = 5000)
    {
        var dbConnection = Context.Database.GetDbConnection();
        if (dbConnection.State != ConnectionState.Open)
            await dbConnection.OpenAsync();

        var tableName = GetTableName();
        var sql = $@"
            SELECT * FROM {tableName} WITH (NOLOCK)
            WHERE Rating > 0
            ORDER BY RecommendedAt DESC";

        var processed = 0;
        await foreach (var recommendation in dbConnection.QueryUnbufferedAsync<UserRecommendation>(sql))
        {
            yield return recommendation;
            processed++;

            if (processed % 10000 != 0)
            {
                continue;
            }

            Console.WriteLine($"📈 Streamed {processed:N0} recommendations with ratings");

            // Periodic garbage collection for very large datasets
            if (processed % 50000 != 0)
            {
                continue;
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Console.WriteLine($"✅ Total streamed: {processed:N0} recommendations with ratings");
    }

    // OPTIMIZED: Configurable version that can use different strategies
    public async Task<IEnumerable<UserRecommendation>> GetAllWithRatingsConfigurableAsync(QueryOptions? options = null)
    {
        options ??= new QueryOptions { Strategy = QueryStrategy.Standard };

        return options.Strategy switch
        {
            QueryStrategy.Standard => await GetAllWithRatingsAsync(),
            QueryStrategy.Chunked => await GetAllWithRatingsChunkedAsync(options.ChunkSize),
            QueryStrategy.MemoryMapped => await GetAllWithRatingsMemoryMappedAsync(),
            _ => await GetAllWithRatingsAsync()
        };
    }

    // OPTIMIZED: Memory-mapped approach for extremely large datasets
    private async Task<IEnumerable<UserRecommendation>> GetAllWithRatingsMemoryMappedAsync()
    {
        var dbConnection = Context.Database.GetDbConnection();
        if (dbConnection.State != ConnectionState.Open)
            await dbConnection.OpenAsync();

        var tableName = GetTableName();
        var sql = $@"
            SELECT * FROM {tableName} WITH (NOLOCK)
            WHERE Rating > 0
            ORDER BY RecommendedAt DESC";

        var results = new List<UserRecommendation>();
        var processed = 0;

        // Use Dapper's buffered query with custom buffer management
        await foreach (var recommendation in dbConnection.QueryUnbufferedAsync<UserRecommendation>(sql))
        {
            results.Add(recommendation);
            processed++;

            if (processed % 10000 == 0)
            {
                Console.WriteLine($"📈 Processed {processed:N0} recommendations with ratings");

                // Force garbage collection periodically to manage memory
                if (processed % 50000 == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
        }

        Console.WriteLine($"✅ Total processed: {processed:N0} recommendations with ratings");
        return results;
    }

    public async Task<IEnumerable<Guid>> GetMostPopularServicesAsync(int count)
    {
        // Use EF for this as it's a simple aggregation query
        return await context.UserRecommendations
            .GroupBy(r => r.ServiceId)
            .OrderByDescending(g => g.Count())
            .Take(count)
            .Select(g => g.Key)
            .ToListAsync();
    }

    public async Task AddRatingAsync(Guid userId, Guid serviceId, float rating)
    {
        var existing = await GetLatestByUserAndServiceAsync(userId, serviceId);
        if (existing != null)
        {
            existing.Rating = rating;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var recommendation = new UserRecommendation
            {
                UserId = userId, ServiceId = serviceId, Rating = rating, RecommendedAt = DateTime.UtcNow
            };
            await AddAsync(recommendation);
        }

        await SaveChangesAsync();
    }

    // Helper method to get the table name from the EF Core model
    private string GetTableName()
    {
        var entityType = context.Model.FindEntityType(typeof(UserRecommendation));
        if (entityType == null)
            throw new InvalidOperationException(
                $"Could not find entity type metadata for {nameof(UserRecommendation)}");

        var schema = entityType.GetSchema();
        var table = entityType.GetTableName();

        return !string.IsNullOrWhiteSpace(schema) ? $"{schema}.{table}" : table!;
    }
}
