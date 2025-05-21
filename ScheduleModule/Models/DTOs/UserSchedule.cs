using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace TBD.ScheduleModule.Models.DTOs;

public class UserSchedule
{
    private double? _overtime;
    private double? _overtimeRate;
    private double? _totalPay;

    public Guid UserId { get; set; }
    public double? TotalHoursWorked { get; set; }
    public double? BasePay { get; set; }

    // Calculated properties are now computed once when needed
    public double? Overtime => _overtime ??= TotalHoursWorked > 40 ? TotalHoursWorked - 40 : 0;

    public double? OvertimeRate => _overtimeRate ??= Overtime > 0 ? BasePay * 1.5 * Overtime : 0;

    public double? TotalPay => _totalPay ??= TotalHoursWorked > 40
        ? OvertimeRate + (BasePay * TotalHoursWorked)
        : BasePay * TotalHoursWorked;

    [Column(TypeName = "nvarchar(max)")] 
    public string DaysWorkedJson { get; set; } = "{}";

    [NotMapped]
    public Dictionary<string, int> DaysWorked
    {
        get
        {
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, int>>(DaysWorkedJson) ??
                       new Dictionary<string, int>();
            }
            catch
            {
                throw new InvalidOperationException("Invalid JSON format for DaysWorkedJson.");
            }
        }
        set => DaysWorkedJson = JsonSerializer.Serialize(value ?? new Dictionary<string, int>());
    }

    public void RecalculateTotalHours()
    {
        TotalHoursWorked = DaysWorked?.Values.Sum();
        ResetCalculatedValues(); // Reset cached properties if input changes
    }

    private void ResetCalculatedValues()
    {
        _overtime = null;
        _overtimeRate = null;
        _totalPay = null;
    }
}