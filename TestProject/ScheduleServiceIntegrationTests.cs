using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using TBD.Data;
using TBD.Models.Entities;
using TBD.Services;

namespace TBD.TestProject;

[TestFixture]
public class ScheduleServiceIntegrationTests
{
    private DbContextOptions<GenericDatabaseContext> _options;
    private GenericDatabaseContext _context;
    private ScheduleService _scheduleService;
    private User _testUser;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _options = new DbContextOptionsBuilder<GenericDatabaseContext>()
            .UseInMemoryDatabase(databaseName: "ScheduleTestDb")
            .Options;
    }

    private ScheduleService CreateService(GenericDatabaseContext context)
    {
        return new ScheduleService(context);
    }
    [SetUp]
    public async Task Setup()
    {
        // Create a fresh context and service for each test
        _context = new GenericDatabaseContext(_options);
        await _context.Database.EnsureDeletedAsync(); // Start with a clean database
        await _context.Database.EnsureCreatedAsync();

        _scheduleService = CreateService(_context);

        // Create a test user
        _testUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com"
        };

        await _context.Users.AddAsync(_testUser);
        await _context.SaveChangesAsync();

        // Seed test data
        var schedules = new List<Schedule>
        {
            new Schedule
            {
                Id = Guid.NewGuid(),
                UserId = _testUser.Id,
                User = _testUser,
                BasePay = 20.0,
                TotalHoursWorked = 40.0,
                DaysWorkedJson = "{\"Monday\":8,\"Tuesday\":8,\"Wednesday\":8,\"Thursday\":8,\"Friday\":8}"
            },
            new Schedule
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                BasePay = 25.0,
                TotalHoursWorked = 35.0,
                DaysWorkedJson = "{\"Monday\":7,\"Tuesday\":7,\"Wednesday\":7,\"Thursday\":7,\"Friday\":7}"
            },
            new Schedule
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                BasePay = 30.0,
                TotalHoursWorked = 20.0,
                DaysWorkedJson = "{\"Monday\":4,\"Tuesday\":4,\"Wednesday\":4,\"Thursday\":4,\"Friday\":4}"
            }
        };

        await _context.Schedules.AddRangeAsync(schedules);
        await _context.SaveChangesAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.DisposeAsync();
    }

    [Test]
    public async Task GetAllAsync_ReturnsAllSchedules()
    {
        // Act
        var result = await _scheduleService.GetAllAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(3));
    }

    [Test]
    public async Task FindAsync_WithValidExpression_ReturnsMatchingSchedules()
    {
        // Act
        var result = await _scheduleService.FindAsync(s => s.BasePay >= 25.0);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.All(s => s.BasePay >= 25.0), Is.True);
    }

    [Test]
    public async Task GetByIdAsync_WithValidId_ReturnsSchedule()
    {
        // Arrange
        var existingSchedule = await _context.Schedules.FirstAsync();

        // Act
        var result = await _scheduleService.GetByIdAsync(existingSchedule.Id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(existingSchedule.Id));
    }

    [Test]
    public async Task FindByWorkDayAsync_WithMatchingSchedule_ReturnsSchedule()
    {
        // Arrange
        var schedule = new Schedule
        {
            DaysWorkedJson = "{\"Monday\":8,\"Tuesday\":8,\"Wednesday\":8,\"Thursday\":8,\"Friday\":8}"
        };

        // Act
        var result = await _scheduleService.FindByWorkDayAsync(schedule);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.DaysWorkedJson, Is.EqualTo(schedule.DaysWorkedJson));
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
        Assert.That(matches.Count, Is.EqualTo(0)); 
        Assert.That(nonMatches.Count, Is.EqualTo(3)); // original 3
    }


    [Test]
    public async Task AddAsync_AddsNewSchedule()
    {
        // Arrange
        var newSchedule = new Schedule
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            BasePay = 22.0,
            TotalHoursWorked = 30.0,
            DaysWorkedJson = "{\"Monday\":6,\"Tuesday\":6,\"Wednesday\":6,\"Thursday\":6,\"Friday\":6}"
        };

        // Act
        await _scheduleService.AddAsync(newSchedule);

        // Assert
        var result = await _context.Schedules.FindAsync(newSchedule.Id);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(newSchedule.Id));
        Assert.That(result.BasePay, Is.EqualTo(22.0));
    }

    [Test]
    public async Task UpdateAsync_UpdatesExistingSchedule()
    {
        // Arrange
        var existingSchedule = await _context.Schedules.FirstAsync();
        existingSchedule.BasePay = 27.5;

        // Act
        await _scheduleService.UpdateAsync(existingSchedule);

        // Assert
        _context.Entry(existingSchedule).State = EntityState.Detached; // Detach to force reload
        var result = await _context.Schedules.FindAsync(existingSchedule.Id);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.BasePay, Is.EqualTo(27.5));
    }

    [Test]
    public async Task RemoveAsync_RemovesSchedule()
    {
        // Arrange
        var existingSchedule = await _context.Schedules.FirstAsync();
        var scheduleCount = await _context.Schedules.CountAsync();

        // Act
        await _scheduleService.RemoveAsync(existingSchedule);

        // Assert
        var newCount = await _context.Schedules.CountAsync();
        Assert.That(newCount, Is.EqualTo(scheduleCount - 1));

        var result = await _context.Schedules.FindAsync(existingSchedule.Id);
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task RecalculateTotalHours_UpdatesHoursCorrectly()
    {
        // Arrange
        var schedule = new Schedule(_testUser)
        {
            DaysWorkedJson = "{\"Monday\":8,\"Tuesday\":7,\"Wednesday\":9}"
        };

        // Act
        schedule.RecalculateTotalHours();
        await _scheduleService.AddAsync(schedule);

        // Assert
        var result = await _context.Schedules.FindAsync(schedule.Id);
        Assert.That(result.TotalHoursWorked, Is.EqualTo(24));
    }
}