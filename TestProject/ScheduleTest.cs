using System;
using System.Collections.Generic;
using NUnit.Framework;
using TBD.ScheduleModule.Data;
using TBD.ScheduleModule.Models;
using TBD.UserModule.Models;

namespace TBD.TestProject;

[TestOf(typeof(Schedule))]
[TestFixture]
public class ScheduleTest(ScheduleDbContext context)
{
    #region Construction & Defaults

    [Test]
    public void Constructor_Default_SetsDefaults()
    {
        var schedule = new Schedule();
        ;

        Assert.That(schedule.TotalHoursWorked, Is.Null);
        Assert.That(schedule.BasePay, Is.Null);
        Assert.That(schedule.DaysWorkedJson, Is.EqualTo("{}"));
    }

    [Test]
    public void Constructor_WithUser_SetsUserProperties()
    {
        var userMock = new User
        {
            Id = Guid.NewGuid(),
            Username = "User1",
            Password = "<PASSWORD>",
            Email = "user1@example.com",
            Schedule = null
        };

        var schedule = new Schedule { UserId = userMock.Id, User = userMock };

        Assert.That(schedule.UserId, Is.EqualTo(userMock.Id));
        Assert.That(schedule.User, Is.EqualTo(userMock));
    }

    #endregion

    #region Overtime Logic

    [TestCase(41f, 1)]
    [TestCase(50f, 10)]
    [TestCase(60f, 20)]
    [TestCase(39.5f, 0)]
    [TestCase(null, 0)]
    public void Overtime_CorrectlyCalculates(float? totalHours, double expectedOvertime)
    {
        var schedule = new Schedule { TotalHoursWorked = totalHours };
        Assert.That(schedule.Overtime, Is.EqualTo(expectedOvertime));
    }

    [Test]
    public void OvertimeRate_WhenOvertimeExists_CalculatesCorrectly()
    {
        var schedule = new Schedule { TotalHoursWorked = 50, BasePay = 20 };
        Assert.That(schedule.OvertimeRate, Is.EqualTo(300));
    }

    [Test]
    public void OvertimeRate_WhenNoOvertime_ReturnsZero()
    {
        var schedule = new Schedule { TotalHoursWorked = 30, BasePay = 20 };
        Assert.That(schedule.OvertimeRate, Is.EqualTo(0));
    }

    #endregion

    #region Total Pay Calculation

    [Test]
    public void TotalPayComputed_WhenTotalHoursWorkedIsLessThanOrEqualTo40_ReturnsCorrectValue()
    {
        var schedule = new Schedule { TotalHoursWorked = 40, BasePay = 20 };
        Assert.That(schedule.TotalPayComputed, Is.EqualTo(800).Within(0.01));
    }

    [Test]
    public void TotalPayComputed_WhenTotalHoursWorkedIsMoreThan40_ReturnsCorrectValue()
    {
        var schedule = new Schedule { TotalHoursWorked = 45, BasePay = 20 };
        Assert.That(schedule.TotalPayComputed, Is.EqualTo(950).Within(0.01));
    }

    [Test]
    public void TotalPayComputed_WhenBasePayIsNull_ReturnsZero()
    {
        var schedule = new Schedule { TotalHoursWorked = 45, BasePay = null };
        Assert.That(schedule.TotalPayComputed, Is.EqualTo(0));
    }

    [Test]
    public void FullCalculation_EndToEnd_ReturnsExpectedTotalPay()
    {
        var schedule = new Schedule
        {
            BasePay = 20,
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 10 },
                { "Tuesday", 10 },
                { "Wednesday", 10 },
                { "Thursday", 10 },
                { "Friday", 10 }
            }
        };

        schedule.RecalculateTotalHours();

        Assert.That(schedule.TotalPayComputed, Is.EqualTo(1100).Within(0.01));
    }

    #endregion

    #region DaysWorked JSON Handling

    [Test]
    public void DaysWorked_Get_DeserializesCorrectly()
    {
        var schedule = new Schedule { DaysWorkedJson = "{\"Monday\":8,\"Tuesday\":9}" };
        var daysWorked = schedule.DaysWorked;

        Assert.That(daysWorked.Count, Is.EqualTo(2));
        Assert.That(daysWorked["Monday"], Is.EqualTo(8));
        Assert.That(daysWorked["Tuesday"], Is.EqualTo(9));
    }

    [Test]
    public void DaysWorked_Set_SerializesCorrectly()
    {
        var schedule = new Schedule();
        var daysWorkedDictionary = new Dictionary<string, int> { { "Monday", 8 }, { "Tuesday", 9 } };

        schedule.DaysWorked = daysWorkedDictionary;

        Assert.That(schedule.DaysWorkedJson, Is.EqualTo("{\"Monday\":8,\"Tuesday\":9}"));
    }

    [Test]
    public void DaysWorked_Get_WhenInvalidJson_ThrowsInvalidOperationException()
    {
        var schedule = new Schedule { DaysWorkedJson = "{invalid json}" };
        Assert.That(() => _ = schedule.DaysWorked, Throws.TypeOf<InvalidOperationException>());
    }

    #endregion

    #region Method Behavior

    [Test]
    public void RecalculateTotalHours_CalculatesCorrectly()
    {
        var schedule = new Schedule { DaysWorked = new Dictionary<string, int> { { "Monday", 8 }, { "Tuesday", 9 } } };

        schedule.RecalculateTotalHours();

        Assert.That(schedule.TotalHoursWorked, Is.EqualTo(17));
    }

    [Test]
    public void RecalculateTotalHours_OverwritesExistingTotal()
    {
        var schedule = new Schedule
        {
            TotalHoursWorked = 100, DaysWorked = new Dictionary<string, int> { { "Monday", 5 }, { "Tuesday", 5 } }
        };

        schedule.RecalculateTotalHours();

        Assert.That(schedule.TotalHoursWorked, Is.EqualTo(10));
    }

    #endregion

    #region Property Tests

    [Test]
    public void BasePay_Property_SetAndGet_WorksCorrectly()
    {
        var schedule = new Schedule { BasePay = 25.75f };
        Assert.That(schedule.BasePay, Is.EqualTo(25.75));
    }

    [Test]
    public void TotalHoursWorked_Property_SetAndGet_WorksCorrectly()
    {
        var schedule = new Schedule { TotalHoursWorked = 39.5f };
        Assert.That(schedule.TotalHoursWorked, Is.EqualTo(39.5));
    }

    #endregion
}
