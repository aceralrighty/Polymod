using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace TBD.Models.Entities;

[Table("Schedule")]
public class Schedule : GenericEntity
{
    // Default constructor for EF Core
    public Schedule() 
    {
    }
    
    // Constructor with user parameter
    public Schedule(User user)
    {
        User = user;
        UserId = user.Id;
    }

    public double? TotalHoursWorked { get; set; }

    // The actual foreign key property
    public Guid UserId { get; set; }

    // Navigation property with ForeignKey attribute referencing the property name
    [ForeignKey("UserId")]
    [Required]
    public User User { get; set; }

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

    // Storage property for serialized dictionary
    [Column(TypeName = "nvarchar(max)")]
    public string DaysWorkedJson { get; set; } = "{}";

    // Non-mapped dictionary property with get/set for easy access
    [NotMapped]
    public Dictionary<string, int> DaysWorked
    {
        get => (string.IsNullOrEmpty(DaysWorkedJson) 
            ? new Dictionary<string, int>() 
            : JsonSerializer.Deserialize<Dictionary<string, int>>(DaysWorkedJson)) ?? throw new InvalidOperationException();
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