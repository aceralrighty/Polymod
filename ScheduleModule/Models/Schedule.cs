using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using TBD.GenericDBProperties;
using TBD.UserModule.Models;

namespace TBD.ScheduleModule.Models;

[Table("Schedule")]
public sealed class Schedule : BaseTableProperties
{
    public double? TotalHoursWorked { get; set; }

    public Schedule()
    {
    }

    public Schedule(User user)
    {
        UserId = user.Id;
        User = user;
    }


    public Guid UserId { get; set; }

    // Navigation property with ForeignKey attribute referencing the property name
    [ForeignKey("UserId")] [Required] public User User { get; set; }

    public double? BasePay { get; set; }

    public double? Overtime
    {
        get
        {
            if (TotalHoursWorked is > 40)
            {
                return TotalHoursWorked - 40;
            }

            return 0;
        }
    }

    public double? OvertimeRate
    {
        get
        {
            if (Overtime.HasValue)
            {
                return BasePay * 1.5 * Overtime.Value;
            }

            return 0;
        }
    }


    public double? TotalPay { get; set; }

    [NotMapped]
    public double TotalPayComputed => TotalHoursWorked <= 40
        ? BasePay.GetValueOrDefault() * TotalHoursWorked.GetValueOrDefault()
        : (BasePay.GetValueOrDefault() * 40) +
          ((BasePay.GetValueOrDefault() * 1.5) * (TotalHoursWorked.GetValueOrDefault() - 40));


    [Column(TypeName = "nvarchar(9)")] public string DaysWorkedJson { get; set; } = "{}";

    /// <summary>
    /// Represents a dictionary containing the days of work and corresponding hours worked for each day.
    /// Maps to a serialized JSON string in the database.
    /// </summary>
    /// <remarks>
    /// The property is not mapped to a database column directly. Instead, it uses the
    /// <see cref="DaysWorkedJson"/> property to store its data as a serialized JSON string.
    /// This provides a way to work with structured data while persisting it in a single column.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when deserialization of the JSON string into a dictionary fails.
    /// </exception>
    [NotMapped]
    public Dictionary<string, int> DaysWorked
    {
        get
        {
            if (string.IsNullOrEmpty(DaysWorkedJson))
            {
                return new Dictionary<string, int>();
            }

            try
            {
                Dictionary<string, int>? result = JsonSerializer.Deserialize<Dictionary<string, int>>(DaysWorkedJson);
                if (result == null)
                {
                    throw new InvalidOperationException("Deserialization resulted in a null dictionary.");
                }

                return result;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Failed to deserialize DaysWorkedJson due to invalid JSON.", ex);
            }
        }
        set => DaysWorkedJson = JsonSerializer.Serialize(value ?? new Dictionary<string, int>());
    }

    public void RecalculateTotalHours()
    {
        TotalHoursWorked = 0;
        Dictionary<string, int> daysWorked = DaysWorked; // Use the property to deserialize
        foreach (int hours in daysWorked.Values)
        {
            TotalHoursWorked += hours;
        }
    }
}
