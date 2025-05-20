using TBD.Repository.Base;

namespace TBD.Repository.Schedule;

public interface IScheduleService:IGenericRepository<Models.Entities.Schedule>
{
    Task<Models.Entities.Schedule> GroupAllUsersByWorkDayAsync(Models.Entities.Schedule schedule);
    
    Task<(List<Models.Entities.Schedule> Matches, List<Models.Entities.Schedule> NonMatches)> GetAllByWorkDayAsync(Models.Entities.Schedule schedule);
    Task<Models.Entities.Schedule> FindByWorkDayAsync(Models.Entities.Schedule schedule);

    IQueryable<Models.Entities.Schedule> GetQueryable();
}