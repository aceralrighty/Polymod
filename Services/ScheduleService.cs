using Microsoft.EntityFrameworkCore;
using TBD.Data;
using TBD.Models.Entities;
using TBD.Repository.Base;
using TBD.Repository.Schedule;

namespace TBD.Services;

public class ScheduleService(GenericDatabaseContext context)
    : GenericRepository<Schedule>(context), IScheduleService
{
    public async Task<Schedule> GroupAllUsersByWorkDayAsync(Schedule schedule)
    {
        var query = _dbSet.AsQueryable();
        return await query.FirstOrDefaultAsync();
    }

    public async Task<(List<Schedule> Matches, List<Schedule> NonMatches)> GetAllByWorkDayAsync(Schedule schedule)
    {
        var matching = await _dbSet
            .Where(s => s.DaysWorkedJson == schedule.DaysWorkedJson)
            .ToListAsync();

        var nonMatching = await _dbSet
            .Where(s => s.DaysWorkedJson != schedule.DaysWorkedJson)
            .ToListAsync();

        return (matching, nonMatching);
    }


    public async Task<Schedule> FindByWorkDayAsync(Schedule schedule)
    {
        // Modified to be more testable - create the query then execute it
        var query = _dbSet.AsQueryable();
        var filteredQuery = query.Where(s => s.DaysWorkedJson == schedule.DaysWorkedJson);
        return await filteredQuery.FirstOrDefaultAsync();
    }

    public IQueryable<Schedule> GetQueryable()
    {
        return _dbSet.AsQueryable();
    }
}