using NUnit.Framework;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using PolyMod.UserModule.Services;
using PolyMod.UserModule.Repositories;
using PolyMod.Shared.Utils;
using PolyMod.MetricsModule.Services.Interfaces;
using AutoMapper;
using PolyMod.API.DTOs.Users;
using PolyMod.UserModule.Models;

namespace TestProject.UserModule.Services;

[TestFixture]
[TestOf(typeof(UserService))]
public class UserServiceTest
{
    private Mock<IUserRepository> _userRepositoryMock;
    private Mock<IMapper> _mapperMock;
    private Mock<IHasher> _hasherMock;
    private Mock<IMetricsServiceFactory> _metricsServiceFactoryMock;
    private Mock<IMetricsService> _metricsServiceMock;
    private UserService _service;

    [SetUp]
    public void Setup()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _mapperMock = new Mock<IMapper>();
        _hasherMock = new Mock<IHasher>();
        _metricsServiceFactoryMock = new Mock<IMetricsServiceFactory>();
        _metricsServiceMock = new Mock<IMetricsService>();

        _metricsServiceFactoryMock
            .Setup(factory => factory.CreateMetricsService(It.IsAny<string>()))
            .Returns(_metricsServiceMock.Object);

        _service = new UserService(
            _userRepositoryMock.Object,
            _mapperMock.Object,
            _hasherMock.Object,
            _metricsServiceFactoryMock.Object);
    }

    // Test cases for GetUserByIdAsync
    [Test]
    public async Task GetUserByIdAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Username = "testuser" };
        var userDto = new UserDto { Id = userId, Username = "testuser" };
        _userRepositoryMock.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(user);
        _mapperMock.Setup(mapper => mapper.Map<UserDto>(user)).Returns(userDto);

        // Act
        var result = await _service.GetUserByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(userDto);
        _metricsServiceMock.Verify(m => m.IncrementCounter("user.get_by_id.success"), Times.Once);
    }

    [Test]
    public async Task GetUserByIdAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepositoryMock.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act
        var result = await _service.GetUserByIdAsync(userId);

        // Assert
        result.Should().BeNull();
        _metricsServiceMock.Verify(m => m.IncrementCounter("user.get_by_id.not_found"), Times.Once);
    }

    [Test]
    public void GetUserByIdAsync_ShouldThrowException_WhenRepositoryThrows()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepositoryMock.Setup(repo => repo.GetByIdAsync(userId)).ThrowsAsync(new Exception("Database error"));

        // Act
        Func<Task> act = async () => await _service.GetUserByIdAsync(userId);

        // Assert
        act.Should().ThrowAsync<Exception>().WithMessage("Database error");
        _metricsServiceMock.Verify(m => m.IncrementCounter(It.Is<string>(s => s.Contains("user.get_by_id.error"))),
            Times.Once);
    }

    // Test cases for GetUserByEmailAsync
    [Test]
    public async Task GetUserByEmailAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        const string email = "test@example.com";
        var user = new User { Email = email };
        var userDto = new UserDto { Email = email };
        _userRepositoryMock.Setup(repo => repo.GetByEmailAsync(email)).ReturnsAsync(user);
        _mapperMock.Setup(mapper => mapper.Map<UserDto>(user)).Returns(userDto);

        // Act
        var result = await _service.GetUserByEmailAsync(email);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(userDto);
        _metricsServiceMock.Verify(m => m.IncrementCounter("user.get_by_email.success"), Times.Once);
    }

    [Test]
    public async Task GetUserByEmailAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Arrange
        var email = "unknown@example.com";
        _userRepositoryMock.Setup(repo => repo.GetByEmailAsync(email)).ReturnsAsync((User?)null);

        // Act
        var result = await _service.GetUserByEmailAsync(email);

        // Assert
        result.Should().BeNull();
        _metricsServiceMock.Verify(m => m.IncrementCounter("user.get_by_email.not_found"), Times.Once);
    }

    [Test]
    public async Task GetUserByEmailAsync_ShouldThrowException_WhenRepositoryThrows()
    {
        // Arrange
        var email = "test@example.com";
        _userRepositoryMock.Setup(repo => repo.GetByEmailAsync(email)).ThrowsAsync(new Exception("Database error"));

        // Act
        Func<Task> act = async () => await _service.GetUserByEmailAsync(email);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Database error");
        _metricsServiceMock.Verify(
            m => m.IncrementCounter(It.Is<string>(s => s.Contains("user.get_by_email.error"))), Times.Once);
    }

    // Test cases for CreateUserAsync
    [Test]
    public async Task CreateUserAsync_ShouldCreateUser_WhenValidUserDtoProvided()
    {
        // Arrange
        var userDto = new UserDto { Username = "testuser", Password = "password123" };
        var user = new User { Username = "testuser" };
        _mapperMock.Setup(mapper => mapper.Map<User>(userDto)).Returns(user);
        _hasherMock.Setup(hasher => hasher.HashPassword(userDto.Password)).Returns("hashedpassword");

        // Act
        await _service.CreateUserAsync(userDto);

        // Assert
        _userRepositoryMock.Verify(repo => repo.AddAsync(It.Is<User>(u => u.Password == "hashedpassword")),
            Times.Once);
        _metricsServiceMock.Verify(m => m.IncrementCounter("user.create.success"), Times.Once);
    }

    [Test]
    public async Task CreateUserAsync_ShouldThrowArgumentException_WhenPasswordIsEmpty()
    {
        // Arrange
        var userDto = new UserDto { Username = "testuser", Password = "" };

        // Act
        Func<Task> act = async () => await _service.CreateUserAsync(userDto);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Password cannot be empty");
        _metricsServiceMock.Verify(m => m.IncrementCounter("user.create.password_validation_failed"), Times.Once);
    }

    [Test]
    public async Task CreateUserAsync_ShouldThrowException_WhenRepositoryThrows()
    {
        // Arrange
        var userDto = new UserDto { Username = "testuser", Password = "password123" };
        var user = new User { Username = "testuser" };
        _mapperMock.Setup(mapper => mapper.Map<User>(userDto)).Returns(user);
        _hasherMock.Setup(hasher => hasher.HashPassword(userDto.Password)).Returns("hashedpassword");
        _userRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<User>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        Func<Task> act = async () => await _service.CreateUserAsync(userDto);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Database error");
        _metricsServiceMock.Verify(m => m.IncrementCounter(It.Is<string>(s => s.Contains("user.create.error"))),
            Times.Once);
    }

    // Add more test cases for other methods...
}
