using TBD.ScheduleModule.Models;

namespace TBD.ScheduleModule.Services;

public interface IScheduleService
{
    Task<IEnumerable<Schedule>> GroupAllUsersByWorkDayAsync(Schedule schedule);
    
    Task<IEnumerable<Schedule>> GetAllByWorkDayAsync(Schedule schedule);
    Task<Schedule> FindByWorkDayAsync(Schedule schedule);
    
    Task UpdateHoursAsync(Schedule schedule);
    Task UpdateBasePayAsync(Guid id);
    
}