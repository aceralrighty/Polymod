using TBD.Repository.Services.Base;

namespace TBD.Repository.Services.Schedule;

public interface IScheduleService:IGenericRepository<Models.Entities.Schedule>
{
    Task<Models.Entities.Schedule> GroupAllUsersByWorkDayAsync(Models.Entities.Schedule schedule);
    
    Task<List<IGrouping<bool, Models.Entities.Schedule>>> GetAllByWorkDayAsync(Models.Entities.Schedule schedule);
    Task<Models.Entities.Schedule> FindByWorkDayAsync(Models.Entities.Schedule schedule);
}