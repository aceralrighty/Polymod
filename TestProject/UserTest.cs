using AutoMapper;
using Moq;
using TBD.API.DTOs;
using TBD.ScheduleModule.Models;
using TBD.UserModule.Models;
using TBD.UserModule.Repositories;
using TBD.UserModule.Services;

namespace TBD.TestProject;

[TestFixture]
public class UserServiceTest
{
    private Mock<IUserRepository> _userRepositoryMock;
    private Mock<IMapper> _mapperMock;
    private UserService _userService;

    // Test data factories
    private static User CreateTestUser(Guid? id = null, string email = "test@example.com", string username = "testuser")
    {
        return new User
        {
            Id = id ?? Guid.NewGuid(),
            Email = email,
            Username = username,
            Password = "hashedPassword123",
            Schedule = new Schedule()
        };
    }

    private static UserDto CreateTestUserDto(Guid? id = null, string email = "test@example.com", string username = "testuser")
    {
        return new UserDto
        {
            Id = id ?? Guid.NewGuid(),
            Email = email,
            Username = username,
            Password = "plainPassword123"
        };
    }

    [SetUp]
    public void SetUp()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _mapperMock = new Mock<IMapper>();
        _userService = new UserService(_userRepositoryMock.Object, _mapperMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _userRepositoryMock.Reset();
        _mapperMock.Reset();
    }

    #region GetUserByIdAsync Tests

    [Test]
    public async Task GetUserByIdAsync_WhenUserExists_ReturnsMappedUserDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);
        var expectedUserDto = CreateTestUserDto(userId);

        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);
        _mapperMock.Setup(m => m.Map<UserDto>(user))
            .Returns(expectedUserDto);

        // Act
        var result = await _userService.GetUserByIdAsync(userId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(expectedUserDto));
        _userRepositoryMock.Verify(r => r.GetByIdAsync(userId), Times.Once);
        _mapperMock.Verify(m => m.Map<UserDto>(user), Times.Once);
    }

    [Test]
    public async Task GetUserByIdAsync_WhenUserNotFound_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync((User)null);

        // Act
        var result = await _userService.GetUserByIdAsync(userId);

        // Assert
        Assert.That(result, Is.Null);
        _userRepositoryMock.Verify(r => r.GetByIdAsync(userId), Times.Once);
        _mapperMock.Verify(m => m.Map<UserDto>(It.IsAny<User>()), Times.Never);
    }

    [Test]
    public void GetUserByIdAsync_WhenGuidEmpty_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _userService.GetUserByIdAsync(Guid.Empty));

        Assert.That(ex.Message, Does.Contain("userId"));
    }

    #endregion

    #region GetUserByEmailAsync Tests

    [Test]
    public async Task GetUserByEmailAsync_WhenUserExists_ReturnsMappedUserDto()
    {
        // Arrange
        const string email = "test@example.com";
        var user = CreateTestUser(email: email);
        var expectedUserDto = CreateTestUserDto(email: email);

        _userRepositoryMock.Setup(r => r.GetByEmailAsync(email))
            .ReturnsAsync(user);
        _mapperMock.Setup(m => m.Map<UserDto>(user))
            .Returns(expectedUserDto);

        // Act
        var result = await _userService.GetUserByEmailAsync(email);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Email, Is.EqualTo(email));
        _userRepositoryMock.Verify(r => r.GetByEmailAsync(email), Times.Once);
    }

    [Test]
    public async Task GetUserByEmailAsync_WhenUserNotFound_ReturnsNull()
    {
        // Arrange
        const string email = "notfound@example.com";
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(email))
            .ReturnsAsync((User)null);

        // Act
        var result = await _userService.GetUserByEmailAsync(email);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void GetUserByEmailAsync_WhenEmailInvalid_ThrowsArgumentException(string invalidEmail)
    {
        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _userService.GetUserByEmailAsync(invalidEmail));

        Assert.That(ex.Message, Does.Contain("email"));
    }

    #endregion

    #region GetUserByUsernameAsync Tests

    [Test]
    public async Task GetUserByUsernameAsync_WhenUserExists_ReturnsMappedUserDto()
    {
        // Arrange
        const string username = "testuser";
        var user = CreateTestUser(username: username);
        var expectedUserDto = CreateTestUserDto(username: username);

        _userRepositoryMock.Setup(r => r.GetByUsernameAsync(username))
            .ReturnsAsync(user);
        _mapperMock.Setup(m => m.Map<UserDto>(user))
            .Returns(expectedUserDto);

        // Act
        var result = await _userService.GetUserByUsernameAsync(username);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Username, Is.EqualTo(username));
    }

    [Test]
    public async Task GetUserByUsernameAsync_WhenUserNotFound_ReturnsNull()
    {
        // Arrange
        const string username = "nonexistent";
        _userRepositoryMock.Setup(r => r.GetByUsernameAsync(username))
            .ReturnsAsync((User)null);

        // Act
        var result = await _userService.GetUserByUsernameAsync(username);

        // Assert
        Assert.That(result, Is.Null);
    }

    #endregion

    #region GetUsersAsync Tests

    [Test]
    public async Task GetUsersAsync_WithValidParameters_ReturnsPagedResult()
    {
        // Arrange
        const int page = 2;
        const int pageSize = 5;
        const int totalCount = 15;

        var users = Enumerable.Range(1, pageSize)
            .Select(_ => CreateTestUser())
            .ToList();
        var userDtos = users.Select(u => CreateTestUserDto(u.Id)).ToList();

        _userRepositoryMock.Setup(r => r.GetCountAsync()).ReturnsAsync(totalCount);
        _userRepositoryMock.Setup(r => r.GetPagedAsync(page, pageSize)).ReturnsAsync(users);
        _mapperMock.Setup(m => m.Map<IEnumerable<UserDto>>(users)).Returns(userDtos);

        // Act
        var result = await _userService.GetUsersAsync(page, pageSize);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Items, Has.Count.EqualTo(pageSize));
        Assert.That(result.Page, Is.EqualTo(page));
        Assert.That(result.PageSize, Is.EqualTo(pageSize));
        Assert.That(result.TotalCount, Is.EqualTo(totalCount));
        Assert.That(result.TotalPages, Is.EqualTo(3)); // 15 / 5 = 3
    }

    [Test]
    [TestCase(-1, 10, 1, 50)] // Invalid page
    [TestCase(0, 10, 1, 50)]  // Invalid page
    [TestCase(1, -1, 1, 50)]  // Invalid pageSize
    [TestCase(1, 0, 1, 50)]   // Invalid pageSize
    [TestCase(1, 1000, 1, 50)] // PageSize too large
    public async Task GetUsersAsync_WithInvalidParameters_UsesDefaultValues(
        int inputPage, int inputPageSize, int expectedPage, int expectedPageSize)
    {
        // Arrange
        const int totalCount = 100;
        var users = new List<User>();
        var userDtos = new List<UserDto>();

        _userRepositoryMock.Setup(r => r.GetCountAsync()).ReturnsAsync(totalCount);
        _userRepositoryMock.Setup(r => r.GetPagedAsync(expectedPage, expectedPageSize)).ReturnsAsync(users);
        _mapperMock.Setup(m => m.Map<IEnumerable<UserDto>>(users)).Returns(userDtos);

        // Act
        var result = await _userService.GetUsersAsync(inputPage, inputPageSize);

        // Assert
        Assert.That(result.Page, Is.EqualTo(expectedPage));
        Assert.That(result.PageSize, Is.EqualTo(expectedPageSize));
        _userRepositoryMock.Verify(r => r.GetPagedAsync(expectedPage, expectedPageSize), Times.Once);
    }

    [Test]
    public async Task GetUsersAsync_WhenNoUsers_ReturnsEmptyResult()
    {
        // Arrange
        const int page = 1;
        const int pageSize = 10;
        const int totalCount = 0;

        var emptyUsers = new List<User>();
        var emptyUserDtos = new List<UserDto>();

        _userRepositoryMock.Setup(r => r.GetCountAsync()).ReturnsAsync(totalCount);
        _userRepositoryMock.Setup(r => r.GetPagedAsync(page, pageSize)).ReturnsAsync(emptyUsers);
        _mapperMock.Setup(m => m.Map<IEnumerable<UserDto>>(emptyUsers)).Returns(emptyUserDtos);

        // Act
        var result = await _userService.GetUsersAsync(page, pageSize);

        // Assert
        Assert.That(result.Items, Is.Empty);
        Assert.That(result.TotalCount, Is.EqualTo(0));
        Assert.That(result.TotalPages, Is.EqualTo(0));
    }

    #endregion

    #region GetAllUsersAsync Tests

    [Test]
    public async Task GetAllUsersAsync_WhenUsersExist_ReturnsAllMappedUserDtos()
    {
        // Arrange
        var users = Enumerable.Range(1, 3)
            .Select(_ => CreateTestUser())
            .ToList();
        var expectedUserDtos = users.Select(u => CreateTestUserDto(u.Id)).ToList();

        _userRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);
        _mapperMock.Setup(m => m.Map<IEnumerable<UserDto>>(users)).Returns(expectedUserDtos);

        // Act
        var result = await _userService.GetAllUsersAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result, Is.EqualTo(expectedUserDtos));
    }

    [Test]
    public async Task GetAllUsersAsync_WhenNoUsers_ReturnsEmptyCollection()
    {
        // Arrange
        var emptyUsers = new List<User>();
        var emptyUserDtos = new List<UserDto>();

        _userRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(emptyUsers);
        _mapperMock.Setup(m => m.Map<IEnumerable<UserDto>>(emptyUsers)).Returns(emptyUserDtos);

        // Act
        var result = await _userService.GetAllUsersAsync();

        // Assert
        Assert.That(result, Is.Empty);
    }

    #endregion

    #region CreateUserAsync Tests

    [Test]
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void CreateUserAsync_WhenPasswordInvalid_ThrowsArgumentException(string invalidPassword)
    {
        // Arrange
        var userDto = CreateTestUserDto();
        userDto.Password = invalidPassword;
        var user = CreateTestUser();
        user.Password = invalidPassword;

        _mapperMock.Setup(m => m.Map<User>(userDto)).Returns(user);

        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _userService.CreateUserAsync(userDto));

        Assert.That(ex.Message, Does.Contain("Password cannot be empty"));
    }

    [Test]
    public async Task CreateUserAsync_WhenPasswordAlreadyHashed_DoesNotCreateUser()
    {
        // Arrange
        var userDto = CreateTestUserDto();
        userDto.Password = "plainPassword";
        var user = CreateTestUser();
        user.Password = "plainPassword"; // Same as input, indicating already hashed

        _mapperMock.Setup(m => m.Map<User>(userDto)).Returns(user);

        // Act
        await _userService.CreateUserAsync(userDto);

        // Assert
        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Test]
    public async Task CreateUserAsync_WhenPasswordNotHashed_HashesPasswordAndCreatesUser()
    {
        // Arrange
        var userDto = CreateTestUserDto();
        userDto.Password = "plainPassword";
        var user = CreateTestUser();
        user.Password = "differentPassword"; // Different from input, indicating not hashed

        _mapperMock.Setup(m => m.Map<User>(userDto)).Returns(user);

        // Act
        await _userService.CreateUserAsync(userDto);

        // Assert
        _userRepositoryMock.Verify(r => r.AddAsync(It.Is<User>(u =>
            !string.IsNullOrWhiteSpace(u.Password) &&
            u.Password != "plainPassword")), Times.Once);
    }

    [Test]
    public void CreateUserAsync_WhenUserDtoIsNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _userService.CreateUserAsync(null));
    }

    #endregion

    #region UpdateUserAsync Tests

    [Test]
    public async Task UpdateUserAsync_WithValidUserDto_MapsAndUpdatesUser()
    {
        // Arrange
        var userDto = CreateTestUserDto();
        var user = CreateTestUser(userDto.Id);

        _mapperMock.Setup(m => m.Map<User>(userDto)).Returns(user);

        // Act
        await _userService.UpdateUserAsync(userDto);

        // Assert
        _userRepositoryMock.Verify(r => r.UpdateAsync(user), Times.Once);
        _mapperMock.Verify(m => m.Map<User>(userDto), Times.Once);
    }

    [Test]
    public void UpdateUserAsync_WhenUserDtoIsNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _userService.UpdateUserAsync(null));
    }

    #endregion

    #region DeleteUserAsync Tests

    [Test]
    public async Task DeleteUserAsync_WhenUserExists_GetsAndRemovesUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);

        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act
        await _userService.DeleteUserAsync(userId);

        // Assert
        _userRepositoryMock.Verify(r => r.GetByIdAsync(userId), Times.Once);
        _userRepositoryMock.Verify(r => r.RemoveAsync(user), Times.Once);
    }

    [Test]
    public void DeleteUserAsync_WhenUserNotFound_ThrowsNullReferenceException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User)null);

        // Act & Assert
        var ex = Assert.ThrowsAsync<NullReferenceException>(
            async () => await _userService.DeleteUserAsync(userId));

        Assert.That(ex, Is.Not.Null);
    }

    [Test]
    public void DeleteUserAsync_WhenGuidEmpty_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _userService.DeleteUserAsync(Guid.Empty));

        Assert.That(ex.Message, Does.Contain("userId"));
    }

    #endregion

    #region Integration-style Tests

    [Test]
    public async Task UserWorkflow_CreateUpdateDelete_WorksCorrectly()
    {
        // This test simulates a complete user lifecycle
        // Arrange
        var userId = Guid.NewGuid();
        var originalUserDto = CreateTestUserDto(userId, "original@test.com", "originaluser");
        var updatedUserDto = CreateTestUserDto(userId, "updated@test.com", "updateduser");

        var originalUser = CreateTestUser(userId, "original@test.com", "originaluser");
        var updatedUser = CreateTestUser(userId, "updated@test.com", "updateduser");

        _mapperMock.Setup(m => m.Map<User>(originalUserDto)).Returns(originalUser);
        _mapperMock.Setup(m => m.Map<User>(updatedUserDto)).Returns(updatedUser);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(originalUser);

        // Act & Assert - Create
        await _userService.CreateUserAsync(originalUserDto);
        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);

        // Act & Assert - Update
        await _userService.UpdateUserAsync(updatedUserDto);
        _userRepositoryMock.Verify(r => r.UpdateAsync(updatedUser), Times.Once);

        // Act & Assert - Delete
        await _userService.DeleteUserAsync(userId);
        _userRepositoryMock.Verify(r => r.RemoveAsync(originalUser), Times.Once);
    }

    #endregion
}
