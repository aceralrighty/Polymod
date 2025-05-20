using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using NUnit.Framework;
using TBD.Data;
using TBD.Models.Entities;
using TBD.Services;

namespace TBD.TestProject;

[TestFixture]
public class ScheduleServiceTestsAlternative
{
    private DbContextOptions<GenericDatabaseContext> _options;
    private ScheduleService _scheduleService;
    private GenericDatabaseContext _context;
    private List<Schedule> _scheduleData;
    private User _testUser;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Create a unique database name to avoid conflicts between test runs
        var dbName = $"ScheduleTest_{Guid.NewGuid()}";

        _options = new DbContextOptionsBuilder<GenericDatabaseContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        // Setup test data
        _testUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com"
        };

        _scheduleData = new List<Schedule>
        {
            new Schedule
            {
                Id = Guid.NewGuid(), UserId = _testUser.Id, DaysWorkedJson = "{\"Monday\":8,\"Tuesday\":8}",
                BasePay = 20.0, TotalHoursWorked = 16.0
            },
            new Schedule
            {
                Id = Guid.NewGuid(), UserId = Guid.NewGuid(), DaysWorkedJson = "{\"Wednesday\":8,\"Thursday\":8}",
                BasePay = 22.0, TotalHoursWorked = 16.0
            },
            new Schedule
            {
                Id = Guid.NewGuid(), UserId = Guid.NewGuid(), DaysWorkedJson = "{\"Monday\":8,\"Tuesday\":8}",
                BasePay = 25.0, TotalHoursWorked = 16.0
            }
        };
    }

    [SetUp]
    public async Task Setup()
    {
        _context = new GenericDatabaseContext(_options);

        // Clear the database
        _context.Schedules.RemoveRange(await _context.Schedules.ToListAsync());
        _context.Users.RemoveRange(await _context.Users.ToListAsync());
        await _context.SaveChangesAsync();

        // Fresh data for each test
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com"
        };

        var scheduleData = new List<Schedule>
        {
            new Schedule
            {
                Id = Guid.NewGuid(), UserId = testUser.Id, User = testUser,
                DaysWorkedJson = "{\"Monday\":8,\"Tuesday\":8}",
                BasePay = 20.0, TotalHoursWorked = 16.0
            },
            new Schedule
            {
                Id = Guid.NewGuid(), UserId = Guid.NewGuid(),
                DaysWorkedJson = "{\"Wednesday\":8,\"Thursday\":8}",
                BasePay = 22.0, TotalHoursWorked = 16.0
            },
            new Schedule
            {
                Id = Guid.NewGuid(), UserId = Guid.NewGuid(),
                DaysWorkedJson = "{\"Monday\":8,\"Tuesday\":8}",
                BasePay = 25.0, TotalHoursWorked = 16.0
            }
        };

        await _context.Users.AddAsync(testUser);
        await _context.Schedules.AddRangeAsync(scheduleData);
        await _context.SaveChangesAsync();

        _scheduleService = new ScheduleService(_context);
    }


    [TearDown]
    public async Task TearDown()
    {
        await _context.DisposeAsync();
    }

    [Test]
    public async Task GroupAllUsersByWorkDayAsync_ReturnsFirstSchedule()
    {
        // Arrange
        var schedule = new Schedule { DaysWorkedJson = "{\"Monday\":8,\"Tuesday\":8}" };

        // Act
        var result = await _scheduleService.GroupAllUsersByWorkDayAsync(schedule);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.DaysWorkedJson, Is.EqualTo(_scheduleData[0].DaysWorkedJson));
    }

    [Test]
    public async Task GetAllByWorkDayAsync_ReturnsGroupedSchedules()
    {
        // Arrange
        var schedule = new Schedule { DaysWorkedJson = "{\"Monday\":8,\"Tuesday\":8}" };

        // Act
        var (matches, nonMatches) = await _scheduleService.GetAllByWorkDayAsync(schedule);

        // Assert
        Assert.That(matches, Is.Not.Null);
        Assert.That(nonMatches, Is.Not.Null);

        Assert.That(matches.Count, Is.EqualTo(2));      // 2 schedules with matching DaysWorkedJson
        Assert.That(nonMatches.Count, Is.EqualTo(1));   // 1 schedule with different DaysWorkedJson
    }


    [Test]
    public async Task GetAllByWorkDayAsync_ReturnsMatchesAndNonMatches()
    {
        // Arrange
        var schedule = new Schedule { DaysWorkedJson = "{\"Monday\":8,\"Tuesday\":8}" };

        // Act
        var result = await _scheduleService.FindByWorkDayAsync(schedule);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.DaysWorkedJson, Is.EqualTo(schedule.DaysWorkedJson));
    }

    [Test]
    public void GetQueryable_ReturnsQueryable()
    {
        // Act
        var result = _scheduleService.GetQueryable();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<IQueryable<Schedule>>());
    }
}