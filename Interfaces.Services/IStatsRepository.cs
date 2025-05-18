using TBD.Models;
using TBD.Repository;

namespace TBD.Interfaces.Services;

public interface IStatsRepository: IGenericRepository<Stats>
{
    List<Stats> GetByUserIdAsync(Guid userId);
    Task<Stats> GroupByWorkoutTypeAsync(Stats workoutType);
}