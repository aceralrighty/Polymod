using Microsoft.EntityFrameworkCore;
using TBD.RecommendationModule.Data;
using TBD.RecommendationModule.Models;
using TBD.RecommendationModule.Repositories.Interfaces;
using TBD.Shared.Repositories;

namespace TBD.RecommendationModule.Repositories;

public class RecommendationOutputRepository(RecommendationDbContext context) : GenericRepository<RecommendationOutput>(context), IRecommendationOutputRepository
{
    public async Task<IEnumerable<RecommendationOutput>> GetLatestRecommendationsForUserAsync(Guid userId, int count = 10)
    {
        return await context.RecommendationOutputs
            .Where(ro => ro.UserId == userId)
            .OrderByDescending(ro => ro.GeneratedAt)
            .Take(count)
            .Include(ro => ro.Service)
            .ToListAsync();
    }

    public async Task<IEnumerable<RecommendationOutput>> GetRecommendationsByBatchAsync(Guid batchId)
    {
        return await context.RecommendationOutputs
            .Where(ro => ro.BatchId == batchId)
            .OrderBy(ro => ro.Rank)
            .Include(ro => ro.Service)
            .ToListAsync();
    }

    public async Task SaveRecommendationBatchAsync(IEnumerable<RecommendationOutput> recommendations)
    {
        var recommendationList = recommendations.ToList();
        if (recommendationList.Count == 0)
        {
            Console.WriteLine("❌ SaveRecommendationBatchAsync: recommendations list is empty");
            return;
        }

        try
        {
            Console.WriteLine($"🔄 SaveRecommendationBatchAsync: Attempting to save {recommendationList.Count} recommendations");

            // Ensure all entities have proper timestamps
            foreach (var rec in recommendationList)
            {
                if (rec.CreatedAt == default)
                    rec.CreatedAt = DateTime.UtcNow;
                if (rec.UpdatedAt == default)
                    rec.UpdatedAt = DateTime.UtcNow;
                if (rec.GeneratedAt == default)
                    rec.GeneratedAt = DateTime.UtcNow;
            }

            // Add all recommendations to the context
            await context.RecommendationOutputs.AddRangeAsync(recommendationList);

            Console.WriteLine($"🔄 SaveRecommendationBatchAsync: Added {recommendationList.Count} entities to context, calling SaveChanges...");

            // Save changes to database
            var savedCount = await context.SaveChangesAsync();

            Console.WriteLine($"✅ SaveRecommendationBatchAsync: Successfully saved {savedCount} recommendation outputs to database");

            // Verify the save worked by checking the database
            var firstRec = recommendationList.First();
            var exists = await context.RecommendationOutputs
                .AnyAsync(ro => ro.Id == firstRec.Id);

            Console.WriteLine($"🔍 SaveRecommendationBatchAsync: Verification check - First recommendation exists: {exists}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ SaveRecommendationBatchAsync: Error saving recommendation outputs: {ex.Message}");
            Console.WriteLine($"❌ SaveRecommendationBatchAsync: Stack trace: {ex.StackTrace}");

            // Log inner exception if it exists
            if (ex.InnerException != null)
            {
                Console.WriteLine($"❌ SaveRecommendationBatchAsync: Inner exception: {ex.InnerException.Message}");
            }

            throw;
        }
    }

    public async Task MarkAsViewedAsync(Guid userId, Guid serviceId)
    {
        var latestRecommendation = await context.RecommendationOutputs
            .Where(ro => ro.UserId == userId && ro.ServiceId == serviceId)
            .OrderByDescending(ro => ro.GeneratedAt)
            .FirstOrDefaultAsync();

        if (latestRecommendation != null && !latestRecommendation.HasBeenViewed)
        {
            latestRecommendation.HasBeenViewed = true;
            latestRecommendation.ViewedAt = DateTime.UtcNow;
            latestRecommendation.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
        }
    }

    public async Task MarkAsClickedAsync(Guid userId, Guid serviceId)
    {
        var latestRecommendation = await context.RecommendationOutputs
            .Where(ro => ro.UserId == userId && ro.ServiceId == serviceId)
            .OrderByDescending(ro => ro.GeneratedAt)
            .FirstOrDefaultAsync();

        if (latestRecommendation != null && !latestRecommendation.HasBeenClicked)
        {
            latestRecommendation.HasBeenClicked = true;
            latestRecommendation.ClickedAt = DateTime.UtcNow;
            latestRecommendation.UpdatedAt = DateTime.UtcNow;

            // If marking as clicked, also mark as viewed if not already
            if (!latestRecommendation.HasBeenViewed)
            {
                latestRecommendation.HasBeenViewed = true;
                latestRecommendation.ViewedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
        }
    }

    public async Task<RecommendationAnalytics> GetRecommendationAnalyticsAsync(Guid userId)
    {
        var outputs = await context.RecommendationOutputs
            .Where(ro => ro.UserId == userId)
            .ToListAsync();

        var totalRecommendations = outputs.Count;
        var viewedRecommendations = outputs.Count(ro => ro.HasBeenViewed);
        var clickedRecommendations = outputs.Count(ro => ro.HasBeenClicked);

        return new RecommendationAnalytics
        {
            UserId = userId,
            TotalRecommendations = totalRecommendations,
            ViewedRecommendations = viewedRecommendations,
            ClickedRecommendations = clickedRecommendations,
            ViewRate = totalRecommendations > 0 ? (double)viewedRecommendations / totalRecommendations : 0,
            ClickThroughRate = viewedRecommendations > 0 ? (double)clickedRecommendations / viewedRecommendations : 0,
            ConversionRate = totalRecommendations > 0 ? (double)clickedRecommendations / totalRecommendations : 0
        };
    }

    public async Task<IEnumerable<RecommendationOutput>> GetRecommendationHistoryAsync(Guid userId, DateTime? since = null)
    {
        var query = context.RecommendationOutputs
            .Where(ro => ro.UserId == userId);

        if (since.HasValue)
        {
            query = query.Where(ro => ro.GeneratedAt >= since.Value);
        }

        return await query
            .OrderByDescending(ro => ro.GeneratedAt)
            .Include(ro => ro.Service)
            .ToListAsync();
    }
}
