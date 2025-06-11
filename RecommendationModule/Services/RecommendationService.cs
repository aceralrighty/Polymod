using Microsoft.EntityFrameworkCore;
using TBD.RecommendationModule.Data;
using TBD.RecommendationModule.Models;
using TBD.RecommendationModule.Repositories;
using TBD.Shared.Repositories;

namespace TBD.RecommendationModule.Services;

public class RecommendationService(RecommendationDbContext context)
    : GenericRepository<Recommendation>(context), IRecommendationRepository
{
    public async Task AddAsync(Recommendation recommendation)
    {
        await context.Recommendations.AddAsync(recommendation);
        await SaveChangesAsync();
    }

    public async Task<IEnumerable<Recommendation>> GetByUserIdAsync(Guid userId)
    {
        return await context.Recommendations.Where(u => u.UserId == userId).OrderByDescending(r => r.RecommendedAt)
            .ToListAsync();
    }

    public async Task<Recommendation?> GetLatestByUserAndServiceAsync(Guid userId, Guid serviceId)
    {
        return await context.Recommendations
            .Where(r => r.UserId == userId && r.ServiceId == serviceId)
            .OrderByDescending(r => r.RecommendedAt)
            .FirstOrDefaultAsync();
    }

    public async Task SaveChangesAsync()
    {
        await context.SaveChangesAsync();
    }
}
