using AutoMapper;
using Moq;
using TBD.API.DTOs;
using TBD.Shared.Utils; // Import the namespace for IHasher
using TBD.UserModule.Models;
using TBD.UserModule.Repositories;
using TBD.UserModule.Services;
using TBD.API.Interfaces;
using Assert = NUnit.Framework.Assert;

namespace TBD.TestProject;

[TestFixture]
public class UserServiceTests
{
    private Mock<IUserRepository> _userRepositoryMock;
    private Mock<IMapper> _mapperMock;
    private Mock<IHasher> _hasherMock; // Add a mock for IHasher
    private IUserService _userService;

    private static readonly UserDto TestUserDto = new()
    {
        Id = Guid.NewGuid(),
        Email = "test@example.com",
        Username = "testuser",
        Password = "plainPassword123"
    };

    [SetUp]
    public void Setup()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _mapperMock = new Mock<IMapper>();
        _hasherMock = new Mock<IHasher>(); // Initialize the hasher mock

        // Instantiate the UserService with the mocks
        _userService = new UserService(
            _userRepositoryMock.Object,
            _mapperMock.Object,
            _hasherMock.Object // Pass the mocked hasher
        );
    }

    [Test]
    public async Task GetUserByIdAsync_UserExists_ReturnsCorrectUser()
    {
        // Arrange
        var user = new User
        {
            Id = TestUserDto.Id,
            Username = TestUserDto.Username,
            Email = TestUserDto.Email,
            Password = "hashedpassword", // Placeholder
            Schedule = new TBD.ScheduleModule.Models.Schedule()
        };
        _userRepositoryMock.Setup(r => r.GetByIdAsync(TestUserDto.Id)).ReturnsAsync(user);
        _mapperMock.Setup(m => m.Map<UserDto>(user)).Returns(TestUserDto);

        // Act
        var result = await _userService.GetUserByIdAsync(TestUserDto.Id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(TestUserDto.Id));
        _userRepositoryMock.Verify(r => r.GetByIdAsync(TestUserDto.Id), Times.Once);
        _mapperMock.Verify(m => m.Map<UserDto>(user), Times.Once);
    }

    [Test]
    public async Task GetUserByEmailAsync_UserExists_ReturnsCorrectUser()
    {
        // Arrange
        var user = new User
        {
            Id = TestUserDto.Id,
            Email = TestUserDto.Email,
            Username = TestUserDto.Username,
            Password = "hashedpassword", // Placeholder
            Schedule = new TBD.ScheduleModule.Models.Schedule()
        };
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(TestUserDto.Email)).ReturnsAsync(user);
        _mapperMock.Setup(m => m.Map<UserDto>(user)).Returns(TestUserDto);

        // Act
        var result = await _userService.GetUserByEmailAsync(TestUserDto.Email);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Email, Is.EqualTo(TestUserDto.Email));
        _userRepositoryMock.Verify(r => r.GetByEmailAsync(TestUserDto.Email), Times.Once);
        _mapperMock.Verify(m => m.Map<UserDto>(user), Times.Once);
    }

    [Test]
    public async Task GetUserByEmailAsync_UserDoesNotExist_ReturnsNull()
    {
        // Arrange
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User)null);
        _mapperMock.Setup(m => m.Map<UserDto>(null)).Returns((UserDto)null);

        // Act
        var result = await _userService.GetUserByEmailAsync("nonexistent@example.com");

        // Assert
        Assert.That(result, Is.Null);
        _userRepositoryMock.Verify(r => r.GetByEmailAsync(It.IsAny<string>()), Times.Once);
        _mapperMock.Verify(m => m.Map<UserDto>(null), Times.Once);
    }

    [Test]
    public async Task GetUserByUsernameAsync_UserExists_ReturnsCorrectUser()
    {
        // Arrange
        var user = new User
        {
            Id = TestUserDto.Id,
            Username = TestUserDto.Username,
            Email = TestUserDto.Email,
            Password = "hashedpassword", // Placeholder
            Schedule = new TBD.ScheduleModule.Models.Schedule()
        };
        _userRepositoryMock.Setup(r => r.GetByUsernameAsync(TestUserDto.Username)).ReturnsAsync(user);
        _mapperMock.Setup(m => m.Map<UserDto>(user)).Returns(TestUserDto);

        // Act
        var result = await _userService.GetUserByUsernameAsync(TestUserDto.Username);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Username, Is.EqualTo(TestUserDto.Username));
        _userRepositoryMock.Verify(r => r.GetByUsernameAsync(TestUserDto.Username), Times.Once);
        _mapperMock.Verify(m => m.Map<UserDto>(user), Times.Once);
    }

    [Test]
    public async Task GetUsersAsync_ValidParameters_ReturnsPagedResult()
    {
        // Arrange
        var users = new List<User>
        {
            new User { Username = "user1", Email = "e1@e.com", Password = "p1", Schedule = new TBD.ScheduleModule.Models.Schedule() },
            new User { Username = "user2", Email = "e2@e.com", Password = "p2", Schedule = new TBD.ScheduleModule.Models.Schedule() }
        };
        var userDtos = new List<UserDto> { new UserDto(), new UserDto() };
        var totalCount = 10;
        int page = 2;
        int pageSize = 5;

        _userRepositoryMock.Setup(r => r.GetCountAsync()).ReturnsAsync(totalCount);
        _userRepositoryMock.Setup(r => r.GetPagedAsync(page, pageSize)).ReturnsAsync(users);
        _mapperMock.Setup(m => m.Map<IEnumerable<UserDto>>(users)).Returns(userDtos);

        // Act
        var result = await _userService.GetUsersAsync(page, pageSize);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Items.Count(), Is.EqualTo(userDtos.Count));
        Assert.That(result.TotalCount, Is.EqualTo(totalCount));
        Assert.That(result.Page, Is.EqualTo(page));
        Assert.That(result.PageSize, Is.EqualTo(pageSize));
        _userRepositoryMock.Verify(r => r.GetCountAsync(), Times.Once);
        _userRepositoryMock.Verify(r => r.GetPagedAsync(page, pageSize), Times.Once);
        _mapperMock.Verify(m => m.Map<IEnumerable<UserDto>>(users), Times.Once);
    }

    [Test]
    public async Task GetUsersAsync_InvalidPage_DefaultsToOne()
    {
        // Arrange
        var users = new List<User>();
        var userDtos = new List<UserDto>();
        var totalCount = 0;
        int invalidPage = 0;
        int pageSize = 10;

        _userRepositoryMock.Setup(r => r.GetCountAsync()).ReturnsAsync(totalCount);
        _userRepositoryMock.Setup(r => r.GetPagedAsync(1, pageSize)).ReturnsAsync(users);
        _mapperMock.Setup(m => m.Map<IEnumerable<UserDto>>(users)).Returns(userDtos);

        // Act
        var result = await _userService.GetUsersAsync(invalidPage, pageSize);

        // Assert
        Assert.That(result.Page, Is.EqualTo(1));
        _userRepositoryMock.Verify(r => r.GetPagedAsync(1, pageSize), Times.Once);
    }

    [Test]
    public async Task GetUsersAsync_InvalidPageSize_DefaultsToFifty()
    {
        // Arrange
        var users = new List<User>();
        var userDtos = new List<UserDto>();
        var totalCount = 0;
        int page = 1;
        int invalidPageSize = 0;

        _userRepositoryMock.Setup(r => r.GetCountAsync()).ReturnsAsync(totalCount);
        _userRepositoryMock.Setup(r => r.GetPagedAsync(page, 50)).ReturnsAsync(users);
        _mapperMock.Setup(m => m.Map<IEnumerable<UserDto>>(users)).Returns(userDtos);

        // Act
        var result = await _userService.GetUsersAsync(page, invalidPageSize);

        // Assert
        Assert.That(result.PageSize, Is.EqualTo(50));
        _userRepositoryMock.Verify(r => r.GetPagedAsync(page, 50), Times.Once);
    }


    [Test]
    public async Task CreateUserAsync_ValidUser_HashesPasswordAndAddsUser()
    {
        // Arrange
        var expectedHashedPassword = "mockedHashedPassword"; // A simple string for the mock

        // Configure the hasher mock to return a predictable hash
        _hasherMock.Setup(h => h.HashPassword(TestUserDto.Password))
                   .Returns(expectedHashedPassword);
        // Configure the hasher mock for verification
        _hasherMock.Setup(h => h.Verify(expectedHashedPassword, TestUserDto.Password))
                   .Returns(true);

        var capturedUser = new User
        {
            Id = TestUserDto.Id,
            Username = TestUserDto.Username,
            Email = TestUserDto.Email,
            Password = "plainPassword123", // This is the plain text password from DTO
            Schedule = new TBD.ScheduleModule.Models.Schedule()
        };

        _mapperMock.Setup(m => m.Map<User>(TestUserDto))
                   .Returns(new User
                   {
                       Password = TestUserDto.Password, // This is the plain text password from DTO
                       Username = TestUserDto.Username,
                       Email = TestUserDto.Email,
                       Schedule = new TBD.ScheduleModule.Models.Schedule()
                   });

        _userRepositoryMock.Setup(r => r.AddAsync(It.IsAny<User>()))
                           .Callback<User>(user => capturedUser = user)
                           .Returns(Task.CompletedTask);

        // Act
        await _userService.CreateUserAsync(TestUserDto);

        // Assert
        // Verify that the hasher's HashPassword method was called with the correct plain text password
        _hasherMock.Verify(h => h.HashPassword(TestUserDto.Password), Times.Once);
        // Verify that the repository's AddAsync method was called exactly once.
        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);

        // Verify that the captured user's password is the mocked hashed password
        Assert.That(capturedUser.Password, Is.EqualTo(expectedHashedPassword));

        // You can now verify that the service attempts to save the (mocked) hashed password.
        // The second assert from your original test (Hasher.Verify) is now redundant here
        // as we are testing the service's interaction with the hasher, not the hasher itself.
        // If you still want to test the full flow with real hashing, keep it as an integration test.
        // For a unit test, the mock verification is sufficient.
    }


    [Test]
    public void CreateUserAsync_NullUserDto_ThrowsAutoMapperException()
    {
        // Arrange
        UserDto nullUserDto = null;
        _mapperMock.Setup(m => m.Map<User>(nullUserDto)).Throws(new AutoMapperMappingException("Mapping null DTO"));

        // Act & Assert
        Assert.ThrowsAsync<AutoMapper.AutoMapperMappingException>(() => _userService.CreateUserAsync(nullUserDto));
        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
        _hasherMock.Verify(h => h.HashPassword(It.IsAny<string>()), Times.Never); // Hasher should not be called
    }

    [Test]
    public void CreateUserAsync_EmptyPassword_ThrowsArgumentException()
    {
        // Arrange
        var invalidUserDto = new UserDto { Password = " ", Username = "test", Email = "test@example.com" };
        _mapperMock.Setup(m => m.Map<User>(invalidUserDto))
                   .Returns(new User
                   {
                       Password = invalidUserDto.Password,
                       Username = invalidUserDto.Username,
                       Email = invalidUserDto.Email,
                       Schedule = new TBD.ScheduleModule.Models.Schedule()
                   });

        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentException>(() => _userService.CreateUserAsync(invalidUserDto));
        Assert.That(ex.Message, Is.EqualTo("Password cannot be empty"));
        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
        _hasherMock.Verify(h => h.HashPassword(It.IsAny<string>()), Times.Never); // Hasher should not be called
    }

    // Removed: CreateUserAsync_FlawedVerificationCheck_StillCreatesUser (as it tests non-existent logic)


    [Test]
    public async Task UpdateUserAsync_ValidUserDto_CallsRepositoryUpdate()
    {
        // Arrange
        var userDto = new UserDto { Id = Guid.NewGuid(), Username = "updatedUser", Email = "updated@example.com", Password = "newPassword" };
        var mappedUser = new User
        {
            Id = userDto.Id,
            Username = userDto.Username,
            Email = userDto.Email,
            Password = userDto.Password,
            Schedule = new TBD.ScheduleModule.Models.Schedule()
        };

        _mapperMock.Setup(m => m.Map<User>(userDto)).Returns(mappedUser);
        _userRepositoryMock.Setup(r => r.UpdateAsync(mappedUser)).Returns(Task.CompletedTask);

        // Act
        await _userService.UpdateUserAsync(userDto);

        // Assert
        _mapperMock.Verify(m => m.Map<User>(userDto), Times.Once);
        _userRepositoryMock.Verify(r => r.UpdateAsync(mappedUser), Times.Once);
    }

    [Test]
    public void UpdateUserAsync_NullUserDto_ThrowsAutoMapperException()
    {
        // Arrange
        UserDto nullUserDto = null;
        _mapperMock.Setup(m => m.Map<User>(nullUserDto)).Throws(new AutoMapper.AutoMapperMappingException("Mapping null DTO"));

        // Act & Assert
        Assert.ThrowsAsync<AutoMapper.AutoMapperMappingException>(() => _userService.UpdateUserAsync(nullUserDto));
        _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Test]
    public async Task DeleteUserAsync_UserExists_RemovesUserFromRepository()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userToDelete = new User
        {
            Id = userId,
            Username = "any",
            Email = "any@any.com",
            Password = "any",
            Schedule = new TBD.ScheduleModule.Models.Schedule()
        };
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(userToDelete);
        _userRepositoryMock.Setup(r => r.RemoveAsync(userToDelete)).Returns(Task.CompletedTask);

        // Act
        await _userService.DeleteUserAsync(userId);

        // Assert
        _userRepositoryMock.Verify(r => r.GetByIdAsync(userId), Times.Once);
        _userRepositoryMock.Verify(r => r.RemoveAsync(userToDelete), Times.Once);
    }

    [Test]
    public async Task DeleteUserAsync_UserNotFound_DoesNotThrow()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _userRepositoryMock.Setup(r => r.GetByIdAsync(nonExistentId)).ReturnsAsync((User)null);
        _userRepositoryMock.Setup(r => r.RemoveAsync(null)).Returns(Task.CompletedTask);

        // Act & Assert
        Assert.DoesNotThrowAsync(() => _userService.DeleteUserAsync(nonExistentId));
        _userRepositoryMock.Verify(r => r.GetByIdAsync(nonExistentId), Times.Once);
        _userRepositoryMock.Verify(r => r.RemoveAsync(null), Times.Once);
    }
}
