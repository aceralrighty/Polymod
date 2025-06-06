using Microsoft.EntityFrameworkCore;
using TBD.ScheduleModule.Data;
using TBD.ScheduleModule.Models;

namespace TBD.ScheduleModule.Repositories;

public class ScheduleRepository(ScheduleDbContext context)
    : GenericScheduleRepository<Schedule>(context), IScheduleRepository
{
    public async Task<Schedule> GetByWorkDayAsync(Schedule schedule)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.DaysWorkedJson == schedule.DaysWorkedJson) ??
               throw new InvalidOperationException();
    }

    public async Task<IEnumerable<Schedule>> GroupByWorkDayAsync(Schedule schedule)
    {
        return await _dbSet.GroupBy(s => s.DaysWorkedJson).Select(s => s.First()).ToListAsync();
    }
}
