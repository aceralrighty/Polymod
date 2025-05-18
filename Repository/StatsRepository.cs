using Microsoft.EntityFrameworkCore;
using TBD.Data;
using TBD.Interfaces.Services;
using TBD.Models;

namespace TBD.Repository;

public class StatsRepository: GenericRepository<Stats>, IStatsRepository
{
    public StatsRepository(GenericDatabaseContext context) : base(context)
    {
    }

    public List<Stats> GetByUserIdAsync(Guid userId)
    {
        return  _dbSet.Where(s => s.Id == userId).ToList();
    }

    public async Task<Stats> GroupByWorkoutTypeAsync(Stats workoutType)
    {
        var groupedStats = await _dbSet
            .GroupBy(s => s.Id) 
            .Select(g => new Stats
            {
                Id = g.Key,
                TotalUsers = g.Sum(s => s.TotalUsers),
                Bike = g.Sum(s => s.Bike),
                Run = g.Sum(s => s.Run),
                Walk = g.Sum(s => s.Walk)
            })
            .FirstOrDefaultAsync(s => s.Id == workoutType.Id);
            
        return groupedStats ?? new Stats { Id = workoutType.Id };
    }
}