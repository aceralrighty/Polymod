using TBD.Repository.Services.Base;

namespace TBD.Repository.Services.Stats;

public interface IStatsRepository: IGenericRepository<Models.Entities.Stats>
{
    List<Models.Entities.Stats> GetByUserIdAsync(Guid userId);
    Task<Models.Entities.Stats> GroupByWorkoutTypeAsync(Models.Entities.Stats workoutType);
}