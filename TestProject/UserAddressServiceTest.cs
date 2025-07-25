using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using TBD.AddressModule.Data;
using TBD.AddressModule.Exceptions;
using TBD.AddressModule.Models;
using TBD.AddressModule.Services;
using TBD.API.DTOs.Users;
using TBD.UserModule.Models;
using TBD.UserModule.Services;

namespace TBD.TestProject;

[TestFixture]
public class UserAddressServiceTests
{
    private AddressDbContext _context;
    private Mock<IMapper> _mockMapper;
    private Mock<IUserService> _mockUserService;
    private UserAddressService _userAddressService;
    private List<UserAddress> _testAddresses;
    private User? _testUser;
    private User? _otherUser;

    [SetUp]
    public async Task Setup()
    {
        // Create an in-memory database with unique name per test
        var options = new DbContextOptionsBuilder<AddressDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AddressDbContext(options);

        _testUser = new User
        {
            Id = Guid.NewGuid(), Email = "testuser@example.com", Password = "testpassword123", Username = "testuser",
        };
        _otherUser = new User
        {
            Id = Guid.NewGuid(), Email = "otheruser@example.com", Password = "password456", Username = "otheruser",
        };

        _testAddresses =
        [
            new UserAddress(_testUser.Id, null, "123 Main St", null, "New York", "NY", "10001") { Id = Guid.NewGuid() },

            new UserAddress(_testUser.Id, null, "456 Oak Ave", "Apt 2", "Los Angeles", "CA", "90210")
            {
                Id = Guid.NewGuid()
            },

            new UserAddress(_testUser.Id, null, "789 Pine Rd", null, "New York", "NY", "10002") { Id = Guid.NewGuid() },

            new UserAddress(_otherUser.Id, null, "1 Other St", null, "Miami", "FL", "33139") { Id = Guid.NewGuid() }
        ];
        var testUserDto = new UserDto { Id = _testUser.Id, Email = _testUser.Email, Password = _testUser.Password, };


        await _context.Set<UserAddress>().AddRangeAsync(_testAddresses);
        await _context.SaveChangesAsync();


        _mockMapper = new Mock<IMapper>();
        _mockUserService = new Mock<IUserService>();

        // Setup UserService mock to simulate finding users. This is the correct way to handle the dependency.
        _mockUserService.Setup(x => x.GetUserByIdAsync(_testUser.Id))
            .ReturnsAsync(testUserDto);

        _mockUserService.Setup(x => x.GetUserByIdAsync(_otherUser.Id))
            .ReturnsAsync(new UserDto { Id = _otherUser.Id });

        // Initialize the service with all dependencies
        _userAddressService = new UserAddressService(_context, _mockMapper.Object, _mockUserService.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    // No other changes are needed below this line. The existing tests will now pass.

    #region GroupByUserStateAsync Tests

    [Test]
    public async Task GroupByUserStateAsync_WithValidData_ReturnsGroupedByState()
    {
        // Act
        var result = await _userAddressService.GroupByUserStateAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(3)); // NY, CA, and FL
        Assert.That(result.Any(g => g.Key == "NY"), Is.True);
        Assert.That(result.Any(g => g.Key == "CA"), Is.True);
        Assert.That(result.Any(g => g.Key == "FL"), Is.True);

        var nyGroup = result.First(g => g.Key == "NY");
        Assert.That(nyGroup.Count(), Is.EqualTo(2));
    }

    #endregion

    #region GetAllAsync Tests

    [Test]
    public async Task GetAllAsync_ReturnsOnlyAddressesForGivenUser()
    {
        if (_testUser != null)
        {
            var result = await _userAddressService.GetAllAsync(_testUser.Id);


            var userAddresses = result.ToList();
            Assert.That(userAddresses, Is.Not.Null);
            Assert.That(userAddresses.Count(), Is.EqualTo(3));
            Assert.That(userAddresses.All(a => a.UserId == _testUser.Id), Is.True);
        }
    }

    #endregion

    #region UpdateUserAddress Tests

    [Test]
    public async Task UpdateUserAddress_WithValidRequest_UpdatesAndReturnsAddress()
    {

        var existingAddress = _testAddresses.First();
        if (_testUser != null)
        {
            var updateRequest = new UserAddressRequest
            {
                Id = existingAddress.Id, UserId = _testUser.Id, Address1 = "Updated Address", City = "Updated City"
            };

            _mockMapper.Setup(m => m.Map(It.IsAny<UserAddressRequest>(), It.IsAny<UserAddress>()))
                .Callback<UserAddressRequest, UserAddress>((src, dest) =>
                {
                    dest.Address1 = src.Address1 ?? string.Empty;
                    dest.City = src.City;
                });


            var result = await _userAddressService.UpdateUserAddress(updateRequest);


            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(existingAddress.Id));
            _mockMapper.Verify(m => m.Map(updateRequest, It.IsAny<UserAddress>()), Times.Once);
        }

        // Verify the address was actually updated in the database
        var updatedAddress = await _context.Set<UserAddress>().FindAsync(existingAddress.Id);
        Assert.That(updatedAddress?.Address1, Is.EqualTo("Updated Address"));
        Assert.That(updatedAddress?.City, Is.EqualTo("Updated City"));
    }

    #endregion
}
