using System.Linq.Expressions;
using TBD.ScheduleModule.Models;

namespace TBD.ScheduleModule.Repositories;

public interface IScheduleRepository
{
    Task<IEnumerable<Schedule>> GetAllAsync();
    Task<IEnumerable<Schedule>> FindAsync(Expression<Func<Schedule, bool>> expression);
    Task<Schedule> GetByWorkDayAsync(Schedule schedule);
    Task<Schedule> GetByIdAsync(Guid id);
    Task UpdateAsync(Schedule schedule);
    Task AddAsync(Schedule schedule);
    Task RemoveAsync(Schedule schedule);

    Task<IEnumerable<Schedule>> GroupByWorkDayAsync(Schedule schedule);

}
