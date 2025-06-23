using TBD.MetricsModule.Services;
using TBD.MetricsModule.Services.Interfaces;
using TBD.ScheduleModule.Models;
using TBD.ScheduleModule.Repositories;

namespace TBD.ScheduleModule.Services;

internal class ScheduleService(IScheduleRepository repository, IMetricsServiceFactory metricsServiceFactory)
    : IScheduleService
{
    private readonly IMetricsService _metricsService = metricsServiceFactory.CreateMetricsService("scheduleModule");

    public async Task<IEnumerable<Schedule>> GroupAllUsersByWorkDayAsync(Schedule schedule)
    {
        _metricsService.IncrementCounter("schedule.group_all_by_workday_count");
        return await repository.GroupByWorkDayAsync(schedule);
    }

    public async Task<IEnumerable<Schedule>> GetAllByWorkDayAsync(Schedule schedule)
    {
        _metricsService.IncrementCounter("schedule.get_all_by_workday_count");
        return await repository.FindAsync(u => u.DaysWorkedJson == schedule.DaysWorkedJson);
    }

    public async Task<Schedule> FindByWorkDayAsync(Schedule schedule)
    {
        _metricsService.IncrementCounter("schedule.find_by_workday_count");
        var worker = await repository.GetByWorkDayAsync(schedule);
        return worker;
    }

    public async Task UpdateHoursAsync(Schedule schedule)
    {
        _metricsService.IncrementCounter("schedule.update_hours_count");
        var worker = await repository.GetByIdAsync(schedule.Id);
        if (worker != null)
        {
            await repository.UpdateAsync(worker);
        }
    }

    public async Task UpdateBasePayAsync(Guid id)
    {
        _metricsService.IncrementCounter("schedule.update_basepay_count");
        var worker = await repository.GetByIdAsync(id);
        if (worker != null)
        {
            await repository.UpdateAsync(worker);
        }
    }
}
