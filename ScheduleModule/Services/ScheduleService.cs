using TBD.ScheduleModule.Models;
using TBD.ScheduleModule.Repositories;

namespace TBD.ScheduleModule.Services;

public class ScheduleService(IScheduleRepository repository) : IScheduleService
{
    public async Task<IEnumerable<Schedule>> GroupAllUsersByWorkDayAsync(Schedule schedule)
    {
        return await repository.GroupByWorkDayAsync(schedule);
    }

    public async Task<IEnumerable<Schedule>> GetAllByWorkDayAsync(Schedule schedule)
    {
        return await repository.FindAsync(u => u.DaysWorkedJson == schedule.DaysWorkedJson);
    }

    public async Task<Schedule> FindByWorkDayAsync(Schedule schedule)
    {
        var worker = await repository.GetByWorkDayAsync(schedule);
        return worker;
    }

    public async Task UpdateHoursAsync(Schedule schedule)
    {
        var worker = await repository.GetByIdAsync(schedule.Id);
        await repository.UpdateAsync(worker);
    }

    public async Task UpdateBasePayAsync(Guid id)
    {
        var worker = await repository.GetByIdAsync(id);
        await repository.UpdateAsync(worker);
    }
}
