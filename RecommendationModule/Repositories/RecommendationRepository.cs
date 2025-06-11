using Microsoft.EntityFrameworkCore;
using TBD.RecommendationModule.Data;
using TBD.RecommendationModule.Models;
using TBD.Shared.Repositories;

namespace TBD.RecommendationModule.Repositories;

public class RecommendationRepository(RecommendationDbContext context)
    : GenericRepository<UserRecommendation>(context), IRecommendationRepository
{
    public async Task AddAsync(UserRecommendation userRecommendation)
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
}
