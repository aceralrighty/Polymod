using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TBD.API.DTOs.AuthDTO;
using TBD.AuthModule.Data;
using TBD.AuthModule.Models;
using TBD.AuthModule.Repositories;
using TBD.AuthModule.Services;
using TBD.MetricsModule.Services;
using TBD.Shared.Utils;

namespace TBD.TestProject;

[TestFixture]
public class AuthServiceTests
{
    private Mock<IAuthRepository> _repositoryMock;
    private DbContextOptions<AuthDbContext> _dbOptions;
    private AuthDbContext _dbContext;
    private IConfiguration _configuration;
    private Mock<ILogger<AuthService>> _loggerMock;
    private Mock<IHasher> _hasherMock;
    private AuthService _authService;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<IAuthRepository>();
        _loggerMock = new Mock<ILogger<AuthService>>();
        _hasherMock = new Mock<IHasher>();

        _dbOptions = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AuthDbContext(_dbOptions);

        var configDict = new Dictionary<string, string>
        {
            { "Jwt:Key", "this-is-a-test-secret-that-should-be-long" },
            { "Jwt:Issuer", "TBD-API" },
            { "Jwt:Audience", "TBD-Client" }
        };
        _configuration = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

        _authService = new AuthService(
            _repositoryMock.Object,
            _dbContext,
            _configuration,
            _loggerMock.Object,
            _hasherMock.Object, new MetricsService("Auth"));
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    [Test]
    public async Task AuthenticateAsync_WithCorrectCredentials_ReturnsUser()
    {
        var user = new AuthUser
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            HashedPassword = "hashed",
            FailedLoginAttempts = 0
        };

        _repositoryMock.Setup(r => r.GetUserByUsername("testuser"))
            .ReturnsAsync(user);
        _hasherMock.Setup(h => h.Verify("hashed", "password"))
            .Returns(true);

        var result = await _authService.AuthenticateAsync("testuser", "password");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Username, Is.EqualTo("testuser"));
        Assert.That(result.FailedLoginAttempts, Is.EqualTo(0));
    }

    [Test]
    public async Task AuthenticateAsync_WithWrongPassword_IncrementsFailedLoginAttempts()
    {
        var user = new AuthUser
        {
            Id = Guid.NewGuid(),
            Username = "wrongpass",
            Email = "wrong@example.com",
            HashedPassword = "hashed",
            FailedLoginAttempts = 0
        };

        _repositoryMock.Setup(r => r.GetUserByUsername("wrongpass"))
            .ReturnsAsync(user);
        _hasherMock.Setup(h => h.Verify("hashed", "badpass"))
            .Returns(false);

        var result = await _authService.AuthenticateAsync("wrongpass", "badpass");

        Assert.That(result, Is.Null);
        _repositoryMock.Verify(r => r.UpdateAsync(It.Is<AuthUser>(u => u.FailedLoginAttempts == 1)), Times.Once);
    }

    [Test]
    public async Task RegisterAsync_WithValidInput_ReturnsSuccess()
    {
        var request = new RegisterRequest
        {
            Username = "newuser", Email = "new@example.com", Password = "securepass123"
        };

        _repositoryMock.Setup(r => r.GetUserByUsername("newuser")).ReturnsAsync((AuthUser?)null);
        _hasherMock.Setup(h => h.HashPassword("securepass123")).Returns("hashedPass");

        var response = await _authService.RegisterAsync(request);

        Assert.That(response.isSuccessful, Is.True);
        Assert.That(response.Message, Is.EqualTo("Registration successful"));
        Assert.That(response.Username, Is.EqualTo("newuser"));
    }

    [Test]
    public async Task RegisterAsync_WithMissingUsername_ReturnsError()
    {
        var request = new RegisterRequest { Username = "", Email = "missing@example.com", Password = "short" };

        var response = await _authService.RegisterAsync(request);

        Assert.That(response.isSuccessful, Is.False);
        Assert.That(response.Message, Does.Contain("All fields"));
    }

    [Test]
    public async Task LoginAsync_WithValidCredentials_ReturnsToken()
    {
        var user = new AuthUser
        {
            Id = Guid.NewGuid(),
            Username = "validuser",
            Email = "valid@example.com",
            HashedPassword = "hashed",
            FailedLoginAttempts = 0
        };

        _repositoryMock.Setup(r => r.GetUserByUsername("validuser")).ReturnsAsync(user);
        _hasherMock.Setup(h => h.Verify("hashed", "password")).Returns(true);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<AuthUser>())).Returns(Task.CompletedTask);

        var request = new LoginRequest { Username = "validuser", Password = "password" };

        var response = await _authService.LoginAsync(request);

        Assert.That(response.isSuccessful, Is.True);
        Assert.That(response.Username, Is.EqualTo("validuser"));
        Assert.That(response.Token, Is.Not.Null.Or.Empty);
        Assert.That(response.RefreshToken, Is.Not.Null.Or.Empty);
    }

    [Test]
    public async Task RefreshTokenAsync_WithExpiredToken_ReturnsFailure()
    {
        var expiredUser = new AuthUser
        {
            Id = Guid.NewGuid(),
            Username = "expireduser",
            HashedPassword = "password",
            Email = "expired@example.com",
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(-1)
        };

        await _dbContext.AuthUsers.AddAsync(expiredUser);
        await _dbContext.SaveChangesAsync();

        var repoMock = new Mock<IAuthRepository>();
        repoMock.Setup(r => r.GetUserByRefreshToken("oldtoken")).ReturnsAsync(expiredUser);

        var authService = new AuthService(repoMock.Object, _dbContext, _configuration, _loggerMock.Object,
            _hasherMock.Object, new MetricsService("Auth"));;
        ;

        var response = await authService.RefreshTokenAsync("oldtoken");

        Assert.That(response.isSuccessful, Is.False);
        Assert.That(response.Message, Does.Contain("expired"));
    }

    [Test]
    public async Task InvalidateRefreshTokenAsync_RemovesTokens()
    {
        var user = new AuthUser
        {
            Id = Guid.NewGuid(),
            Username = "tokenuser",
            HashedPassword = "passowrd",
            Email = "token@example.com",
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(1)
        };

        await _dbContext.AuthUsers.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        await _authService.InvalidateRefreshTokenAsync(user.Id);

        var updated = await _dbContext.AuthUsers.FirstOrDefaultAsync(u => u.Id == user.Id);
        Assert.That(updated?.RefreshToken, Is.Null);
        Assert.That(updated?.RefreshTokenExpiry, Is.Null);
    }
}
