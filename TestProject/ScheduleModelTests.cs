using System.Text.Json;
using NUnit.Framework;
using TBD.Models.Entities;

namespace TBD.TestProject;

[TestFixture]
public class ScheduleModelTests
{
    private User _testUser;
    private Schedule _schedule;

    [SetUp]
    public void Setup()
    {
        _testUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com"
        };

        _schedule = new Schedule(_testUser)
        {
            BasePay = 20.0,
            DaysWorkedJson = JsonSerializer.Serialize(new Dictionary<string, int>
            {
                { "Monday", 8 },
                { "Tuesday", 8 },
                { "Wednesday", 8 },
                { "Thursday", 8 },
                { "Friday", 8 }
            })
        };
        _schedule.RecalculateTotalHours();
    }

    [Test]
    public void Constructor_WithUser_SetsUserAndUserId()
    {
        // Assert
        Assert.That(_schedule.User, Is.EqualTo(_testUser));
        Assert.That(_schedule.UserId, Is.EqualTo(_testUser.Id));
    }

    [Test]
    public void RecalculateTotalHours_SumsHoursFromDaysWorked()
    {
        // Assert
        Assert.That(_schedule.TotalHoursWorked, Is.EqualTo(40));
    }

    [Test]
    public void Overtime_WhenExactly40Hours_ReturnsZero()
    {
        // Assert
        Assert.That(_schedule.Overtime, Is.EqualTo(0));
    }

    [Test]
    public void Overtime_WhenMoreThan40Hours_ReturnsExcessHours()
    {
        // Arrange
        _schedule.TotalHoursWorked = 45;

        // Assert
        Assert.That(_schedule.Overtime, Is.EqualTo(5));
    }

    [Test]
    public void Overtime_WhenLessThan40Hours_ReturnsZero()
    {
        // Arrange
        _schedule.TotalHoursWorked = 35;

        // Assert
        Assert.That(_schedule.Overtime, Is.EqualTo(0));
    }

    [Test]
    public void OvertimeRate_WhenOvertime_CalculatesCorrectly()
    {
        // Arrange
        _schedule.BasePay = 20.0;
        _schedule.TotalHoursWorked = 45;

        // Assert
        Assert.That(_schedule.OvertimeRate, Is.EqualTo(20.0 * 1.5 * 5).Within(0.001));
    }

    [Test]
    public void OvertimeRate_WhenNoOvertime_ReturnsZero()
    {
        // Assert
        Assert.That(_schedule.OvertimeRate, Is.EqualTo(0));
    }

    [Test]
    public void TotalPay_WhenNoOvertime_CalculatesBasePay()
    {
        // Assert
        Assert.That(_schedule.TotalPay, Is.EqualTo(20.0 * 40).Within(0.001));
    }

    [Test]
    public void TotalPay_WhenOvertime_IncludesOvertimeRate()
    {
        // Arrange
        _schedule.TotalHoursWorked = 45;

        // Assert - This should be base pay for all hours plus the overtime premium
        double expected = (20.0 * 45) + (20.0 * 1.5 * 5);
        Assert.That(_schedule.TotalPay, Is.EqualTo(expected).Within(0.001));
    }

    [Test]
    public void DaysWorked_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var daysWorked = new Dictionary<string, int>
        {
            { "Monday", 8 },
            { "Tuesday", 9 }
        };

        // Act
        _schedule.DaysWorkedJson = JsonSerializer.Serialize(daysWorked);
        var result = _schedule.DaysWorked;

        // Assert
        Assert.That(result, Is.EquivalentTo(daysWorked));
        Assert.That(result["Monday"], Is.EqualTo(8));
        Assert.That(result["Tuesday"], Is.EqualTo(9));
    }

    [Test]
    public void DaysWorked_WhenJsonIsEmpty_ReturnsEmptyDictionary()
    {
        // Arrange
        _schedule.DaysWorkedJson = "{}";

        // Act
        var result = _schedule.DaysWorked;

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void DaysWorked_WhenJsonIsNull_ReturnsEmptyDictionary()
    {
        // Arrange
        _schedule.DaysWorkedJson = null;

        // Act
        var result = _schedule.DaysWorked;

        // Assert
        Assert.That(result, Is.Empty);
    }
}