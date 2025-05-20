using Microsoft.EntityFrameworkCore;
using TBD.Data;
using TBD.Models.Entities;
using TBD.Repository.Services.Base;
using TBD.Repository.Services.Schedule;

namespace TBD.Services;

public class ScheduleService(GenericDatabaseContext context)
    : GenericRepository<Schedule>(context), IScheduleService
{
    public async Task<Schedule> GroupAllUsersByWorkDayAsync(Schedule schedule)
    {
        return await _dbSet.FirstOrDefaultAsync();
    }

    public async Task<List<IGrouping<bool, Schedule>>> GetAllByWorkDayAsync(
        Schedule schedule)
    {
        return await _dbSet.GroupBy(s => s.DaysWorked.Keys == schedule.DaysWorked.Keys).ToListAsync();
    }

    public async Task<Schedule> FindByWorkDayAsync(Schedule schedule)
    {
        return await _dbSet.Where(s => s.DaysWorked.Keys == schedule.DaysWorked.Keys).FirstOrDefaultAsync();
    }
}