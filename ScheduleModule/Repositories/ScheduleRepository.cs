using Microsoft.EntityFrameworkCore;
using TBD.ScheduleModule.Data;
using TBD.ScheduleModule.Models;
using TBD.Shared.Repositories;

namespace TBD.ScheduleModule.Repositories;

public class ScheduleRepository(ScheduleDbContext context)
    : GenericRepository<Schedule>(context), IScheduleRepository
{
    public async Task<Schedule> GetByWorkDayAsync(Schedule schedule)
    {
        return await DbSet.FirstOrDefaultAsync(u => u.DaysWorkedJson == schedule.DaysWorkedJson) ??
               throw new InvalidOperationException();
    }

    public async Task RemoveAsync(Schedule schedule)
    {
        context.Remove(schedule);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Schedule>> GroupByWorkDayAsync(Schedule schedule)
    {
        return await DbSet.GroupBy(s => s.DaysWorkedJson).Select(s => s.First()).ToListAsync();
    }
}
