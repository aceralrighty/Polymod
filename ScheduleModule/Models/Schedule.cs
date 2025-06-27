using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using TBD.Shared.GenericDBProperties;
using TBD.UserModule.Models;

namespace TBD.ScheduleModule.Models;

public class Schedule : BaseTableProperties
{
    public float? TotalHoursWorked { get; set; } // Changed from double? to float?

    // Have to explicitly call this constructor because my tests freak out when I don't
    public Schedule() { }

    public Schedule(User user)
    {
        UserId = user.Id;
        User = user;
    }

    public Guid UserId { get; set; }
    [ForeignKey(nameof(UserId))] public User? User { get; set; }

    public float? BasePay { get; set; } // Changed from double? to float?

    /// <summary>
    /// Gets the total overtime hours (anything over 40 hours)
    /// </summary>
    public float? Overtime
    {
        get
        {
            if (TotalHoursWorked > 40)
            {
                return TotalHoursWorked - 40;
            }

            return 0;
        }
    }

    /// <summary>
    /// Gets the overtime hours at 1.5x rate (hours 41-60)
    /// </summary>
    public float? OvertimeRegular
    {
        get
        {
            return TotalHoursWorked switch
            {
                <= 40 => 0,
                <= 60 => TotalHoursWorked - 40,
                _ => 20
            };
        }
    }

    /// <summary>
    /// Gets the overtime hours at 2x rate (hours 61+)
    /// </summary>
    public float? OvertimeDouble
    {
        get
        {
            if (TotalHoursWorked <= 60) return 0;
            return TotalHoursWorked - 60;
        }
    }

    /// <summary>
    /// Calculates the total overtime pay using tiered rates
    /// </summary>
    public float? OvertimeRate
    {
        get
        {
            if (!BasePay.HasValue || !TotalHoursWorked.HasValue || TotalHoursWorked <= 40)
            {
                return 0;
            }

            float overtimePay = 0;

            // Calculate 1.5x overtime pay (hours 41-60)
            var regularOvertimeHours = OvertimeRegular.GetValueOrDefault();
            if (regularOvertimeHours > 0)
            {
                overtimePay += BasePay.Value * 1.5f * regularOvertimeHours;
            }

            // Calculate 2x overtime pay (hours 61+)
            var doubleOvertimeHours = OvertimeDouble.GetValueOrDefault();
            if (doubleOvertimeHours > 0)
            {
                overtimePay += BasePay.Value * 2.0f * doubleOvertimeHours;
            }

            return overtimePay;
        }
    }

    public float? TotalPay { get; set; } // Changed from double? to float?

    /// <summary>
    /// Computes total pay including tiered overtime rates
    /// </summary>
    [NotMapped]
    public float TotalPayComputed // Changed from double to float
    {
        get
        {
            if (!BasePay.HasValue || !TotalHoursWorked.HasValue)
            {
                return 0;
            }

            var regularPay = BasePay.Value * Math.Min(TotalHoursWorked.Value, 40);
            var overtimePay = OvertimeRate.GetValueOrDefault();

            return regularPay + overtimePay;
        }
    }

    [Column(TypeName = "nvarchar(255)")] public string DaysWorkedJson { get; set; } = "{}";

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
                var result = JsonSerializer.Deserialize<Dictionary<string, int>>(DaysWorkedJson);
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
        set => DaysWorkedJson = JsonSerializer.Serialize(value);
    }

    public void RecalculateTotalHours()
    {
        TotalHoursWorked = 0;
        var daysWorked = DaysWorked; // Use the property to deserialize
        foreach (var hours in daysWorked.Values)
        {
            TotalHoursWorked += hours;
        }
    }
}
