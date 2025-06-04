using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using TBD.ScheduleModule.Data;
using TBD.ScheduleModule.Models;
using TBD.UserModule.Models;

namespace TBD.TestProject;

[TestFixture]
[TestOf(typeof(Schedule))]
public class ScheduleTest
{
    private ScheduleDbContext _context;
    [Test]
    public void Constructor_Default_SetsDefaults()
    {
        // Arrange & Act
        var schedule = new Schedule();

        // Assert
        Assert.That(schedule.TotalHoursWorked, Is.Null);
        Assert.That(schedule.BasePay, Is.Null);
        Assert.That(schedule.DaysWorkedJson, Is.EqualTo("{}"));
    }

    [Test]
    public void Constructor_WithUser_SetsUserProperties()
    {
        // Arrange
        var userMock = new User
        {
            Id = Guid.NewGuid(),
            Username = "User1",
            Password = "<PASSWORD>",
            Email = "user1@example.com",
            Schedule = new Schedule()
        };

        // Act
        var schedule = new Schedule(userMock);

        // Assert
        Assert.That(schedule.UserId, Is.EqualTo(userMock.Id));
        Assert.That(schedule.User, Is.EqualTo(userMock));
    }

    [Test]
    public void Overtime_WhenTotalHoursWorkedIsGreaterThan40_ReturnsDifference()
    {
        // Arrange
        var schedule = new Schedule { TotalHoursWorked = 50 };

        // Act
        var overtime = schedule.Overtime;

        // Assert
        Assert.That(overtime, Is.EqualTo(10));
    }

    [Test]
    public void OvertimeRate_WhenOvertimeExists_CalculatesCorrectly()
    {
        // Arrange
        var schedule = new Schedule { TotalHoursWorked = 50, BasePay = 20 };

        // Act
        var overtimeRate = schedule.OvertimeRate;

        // Assert
        Assert.That(overtimeRate, Is.EqualTo(300));
    }

    [Test]
    public void OvertimeRate_WhenNoOvertime_ReturnsZero()
    {
        // Arrange
        var schedule = new Schedule { TotalHoursWorked = 30, BasePay = 20 };

        // Act
        var overtimeRate = schedule.OvertimeRate;

        // Assert
        Assert.That(overtimeRate, Is.EqualTo(0));
    }

    [Test]
    public void TotalPayComputed_WhenTotalHoursWorkedIsLessThanOrEqualTo40_ReturnsCorrectValue()
    {
        // Arrange
        var schedule = new Schedule { TotalHoursWorked = 40, BasePay = 20 };

        // Act
        double totalPay = schedule.TotalPayComputed;

        // Assert
        Assert.That(totalPay, Is.EqualTo(800));
    }


    [Test]
    public void TotalPayComputed_WhenTotalHoursWorkedIsMoreThan40_ReturnsCorrectValue()
    {
        // Arrange
        var schedule = new Schedule { TotalHoursWorked = 45, BasePay = 20 };

        // Act
        double totalPay = schedule.TotalPayComputed;

        // 800 base + 150 overtime (5 * 30)
        Assert.That(totalPay, Is.EqualTo(950));
    }


    [Test]
    public void DaysWorked_Get_DeserializesCorrectly()
    {
        // Arrange
        var schedule = new Schedule { DaysWorkedJson = "{\"Monday\":8,\"Tuesday\":9}" };

        // Act
        var daysWorked = schedule.DaysWorked;

        // Assert
        Assert.That(daysWorked.Count, Is.EqualTo(2));
        Assert.That(daysWorked["Monday"], Is.EqualTo(8));
        Assert.That(daysWorked["Tuesday"], Is.EqualTo(9));
    }

    [Test]
    public void DaysWorked_Set_SerializesCorrectly()
    {
        // Arrange
        var schedule = new Schedule();
        var daysWorkedDictionary = new Dictionary<string, int> { { "Monday", 8 }, { "Tuesday", 9 } };

        // Act
        schedule.DaysWorked = daysWorkedDictionary;

        // Assert
        Assert.That(schedule.DaysWorkedJson, Is.EqualTo("{\"Monday\":8,\"Tuesday\":9}"));
    }

    [Test]
    public void DaysWorked_Get_WhenInvalidJson_ThrowsInvalidOperationException()
    {
        // Arrange
        var schedule = new Schedule { DaysWorkedJson = "{invalid json}" };

        Assert.That(AccessDaysWorked, Throws.TypeOf<InvalidOperationException>());
        return;

        // Act & Assert
        void AccessDaysWorked() => _ = schedule.DaysWorked;
    }

    [Test]
    public void RecalculateTotalHours_CalculatesCorrectly()
    {
        // Arrange
        var schedule = new Schedule
        {
            DaysWorked = new Dictionary<string, int> { { "Monday", 8 }, { "Tuesday", 9 } }
        };

        // Act
        schedule.RecalculateTotalHours();

        // Assert
        Assert.That(schedule.TotalHoursWorked, Is.EqualTo(17));
    }

    [Test]
    public void BasePay_Property_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var schedule = new Schedule { BasePay = 25.75 };

        // Act
        var basePay = schedule.BasePay;

        // Assert
        Assert.That(basePay, Is.EqualTo(25.75));
    }

    [Test]
    public void TotalHoursWorked_Property_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var schedule = new Schedule { TotalHoursWorked = 39.5 };

        // Act
        var totalHoursWorked = schedule.TotalHoursWorked;

        // Assert
        Assert.That(totalHoursWorked, Is.EqualTo(39.5));
    }
}
