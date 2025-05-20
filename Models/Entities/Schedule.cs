using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace TBD.Models.Entities;

[Table("Schedule")]
public class Schedule : GenericEntity
{
    public Schedule()
    {
    }


    public Schedule(User user)
    {
        User = user;
        UserId = user.Id;
    }

    public double? TotalHoursWorked { get; set; }


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

    public double? TotalPay
    {
        get
        {
            if (TotalHoursWorked is > 40)
            {
                return OvertimeRate + (BasePay * TotalHoursWorked);
            }

            return BasePay * TotalHoursWorked;
        }
    }

    
    [Column(TypeName = "nvarchar(max)")] public string DaysWorkedJson { get; set; } = "{}";

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
        get => (string.IsNullOrEmpty(DaysWorkedJson)
                   ? new Dictionary<string, int>()
                   : JsonSerializer.Deserialize<Dictionary<string, int>>(DaysWorkedJson)) ??
               throw new InvalidOperationException();
        init => DaysWorkedJson = JsonSerializer.Serialize(value);
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