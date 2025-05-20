using Microsoft.EntityFrameworkCore;
using TBD.Data;
using TBD.Repository.Services.Base;
using TBD.Repository.Services.Schedule;

namespace TBD.Services;

public class ScheduleService(GenericDatabaseContext context)
    : GenericRepository<Models.Entities.Schedule>(context), IScheduleService
{
    public async Task<Models.Entities.Schedule> GroupAllUsersByWorkDayAsync(Models.Entities.Schedule schedule)
    {
        return await _dbSet.FirstOrDefaultAsync();
    }

    public async Task<List<IGrouping<bool, Models.Entities.Schedule>>> GetAllByWorkDayAsync(
        Models.Entities.Schedule schedule)
    {
        return await _dbSet.GroupBy(s => s.DaysWorked.Keys == schedule.DaysWorked.Keys).ToListAsync();
    }

    public async Task<Models.Entities.Schedule> FindByWorkDayAsync(Models.Entities.Schedule schedule)
    {
        return await _dbSet.Where(s => s.DaysWorked.Keys == schedule.DaysWorked.Keys).FirstOrDefaultAsync();
    }
}