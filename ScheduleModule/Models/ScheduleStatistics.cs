namespace TBD.ScheduleModule.Models;

public record ScheduleStatistics
{
    public double AvgHours { get; init; }
    public double AvgBasePay { get; init; }
    public int RegularTimeCount { get; init; }
    public int OvertimeCount { get; init; }
    public int DoubleOvertimeCount { get; init; }

}
