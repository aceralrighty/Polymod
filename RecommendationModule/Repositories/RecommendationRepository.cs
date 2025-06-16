using Microsoft.EntityFrameworkCore;
using TBD.RecommendationModule.Data;
using TBD.RecommendationModule.Models;
using TBD.RecommendationModule.Repositories.Interfaces;
using TBD.Shared.Repositories;

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

    public async Task<IEnumerable<UserRecommendation>> GetAllWithRatingsAsync()
    {
        return await context.UserRecommendations.Where(r => r.Rating > 0).ToListAsync();
    }

    public async Task<IEnumerable<Guid>> GetMostPopularServicesAsync(int count)
    {
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
                UserId = userId,
                ServiceId = serviceId,
                Rating = rating,
                RecommendedAt = DateTime.UtcNow
            };
            await AddAsync(recommendation);
        }
        await SaveChangesAsync();
    }
}
