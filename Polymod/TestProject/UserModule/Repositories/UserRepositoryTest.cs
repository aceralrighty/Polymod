using NUnit.Framework;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PolyMod.UserModule.Repositories;
using PolyMod.UserModule.Data;
using PolyMod.UserModule.Models;

namespace TestProject.UserModule.Repositories;

[TestFixture]
[TestOf(typeof(UserRepository))]
public class UserRepositoryTest
{
    private UserDbContext _context;
    private UserRepository _userRepository;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new UserDbContext(options);
        _context.Database.EnsureDeleted();
        _context.Database.EnsureCreated();

        _userRepository = new UserRepository(_context);
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userRepository.GetByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
    }

    [Test]
    public async Task GetByIdAsync_ShouldThrowException_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var act = async () => await _userRepository.GetByIdAsync(userId);

        // Assert
        await FluentActions.Awaiting(() => _userRepository.GetByIdAsync(userId))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("User not found");
    }

    [Test]
    public async Task GetByIdsAsync_ShouldReturnUsers_WhenUsersExist()
    {
        // Arrange
        var userIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var users = userIds.Select(id => new User { Id = id }).ToList();
        _context.Users.AddRange(users);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userRepository.GetByIdsAsync(userIds);

        // Assert
        result.Should().HaveCount(userIds.Count);
        result.Select(u => u.Id).Should().BeEquivalentTo(userIds);
    }

    [Test]
    public async Task CreateAsync_ShouldAddUserAndSaveChanges()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid() };

        // Act
        var result = await _userRepository.CreateAsync(user);

        // Assert
        result.Should().BeSameAs(user);
        var found = await _context.Users.FindAsync(user.Id);
        found.Should().NotBeNull();
        found!.Id.Should().Be(user.Id);
    }

    [Test]
    public async Task GetByEmailAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var email = "test@example.com";
        var user = new User { Id = Guid.NewGuid(), Email = email };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userRepository.GetByEmailAsync(email);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be(email);
    }

    [Test]
    public async Task GetByUsernameAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var username = "testuser";
        var user = new User { Id = Guid.NewGuid(), Username = username };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userRepository.GetByUsernameAsync(username);

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be(username);
    }

    [Test]
    public async Task AddRangeAsync_ShouldAddUsersAndSaveChanges()
    {
        // Arrange
        var users = new List<User> { new User { Id = Guid.NewGuid() }, new User { Id = Guid.NewGuid() } };

        // Act
        await _userRepository.AddRangeAsync(users);

        // Assert
        var ids = users.Select(u => u.Id).ToList();
        var fetched = await _context.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
        fetched.Should().HaveCount(users.Count);
    }

    [Test]
    public async Task UpdateAsync_ShouldUpdateUserAndSaveChanges()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "old@example.com" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        user.Email = "new@example.com";

        // Act
        var result = await _userRepository.UpdateAsync(user);

        // Assert
        result.Should().BeSameAs(user);
        var reloaded = await _context.Users.FindAsync(user.Id);
        reloaded!.Email.Should().Be("new@example.com");
    }

    [Test]
    public async Task RemoveAsync_ShouldRemoveUserAndSaveChanges()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid() };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        await _userRepository.RemoveAsync(user);

        // Assert
        var found = await _context.Users.FindAsync(user.Id);
        found.Should().BeNull();
    }

    [Test]
    public async Task GetCountAsync_ShouldReturnUserCount()
    {
        // Arrange
        _context.Users.AddRange(
            new User { Id = Guid.NewGuid() },
            new User { Id = Guid.NewGuid() },
            new User { Id = Guid.NewGuid() }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _userRepository.GetCountAsync();

        // Assert
        result.Should().Be(3);
    }

    [Test]
    public async Task GetPagedAsync_ShouldReturnPagedUsers()
    {
        // Arrange
        var users = Enumerable.Range(0, 5).Select(_ => new User { Id = Guid.NewGuid() }).ToList();
        _context.Users.AddRange(users);
        await _context.SaveChangesAsync();
        var page = 1;
        var pageSize = 2;

        // Act
        var result = await _userRepository.GetPagedAsync(page, pageSize);

        // Assert
        result.Should().HaveCount(pageSize);
    }

    [Test]
    public async Task GetTopAsync_ShouldReturnTopUsers()
    {
        // Arrange
        var users = Enumerable.Range(0, 5).Select(_ => new User { Id = Guid.NewGuid() }).ToList();
        _context.Users.AddRange(users);
        await _context.SaveChangesAsync();
        var count = 2;

        // Act
        var result = await _userRepository.GetTopAsync(count);

        // Assert
        result.Should().HaveCount(count);
    }
}
