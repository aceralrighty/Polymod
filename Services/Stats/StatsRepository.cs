using Microsoft.EntityFrameworkCore;
using TBD.Data;
using TBD.Repository.Base;
using TBD.Repository.Stats;

namespace TBD.Services.Stats;

public class StatsRepository(GenericDatabaseContext context)
    : GenericRepository<Models.Entities.Stats>(context), IStatsRepository
{
    public List<Models.Entities.Stats> GetByUserIdAsync(Guid userId)
    {
        return  _dbSet.Where(s => s.Id == userId).ToList();
    }

    public async Task<Models.Entities.Stats> GroupByWorkoutTypeAsync(Models.Entities.Stats workoutType)
    {
        var groupedStats = await _dbSet
            .GroupBy(s => s.Id) 
            .Select(g => new Models.Entities.Stats
            {
                Id = g.Key,
                TotalUsers = g.Sum(s => s.TotalUsers),
                Bike = g.Sum(s => s.Bike),
                Run = g.Sum(s => s.Run),
                Walk = g.Sum(s => s.Walk)
            })
            .FirstOrDefaultAsync(s => s.Id == workoutType.Id);
            
        return groupedStats ?? new Models.Entities.Stats { Id = workoutType.Id };
    }
}