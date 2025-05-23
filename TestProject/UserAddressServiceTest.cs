using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using TBD.AddressModule.Data;
using TBD.AddressModule.Models;
using TBD.AddressModule.Services;
using TBD.API.DTOs;
using TBD.UserModule.Models;

namespace TBD.TestProject
{
    [TestFixture]
    public class UserAddressServiceTests
    {
        private AddressDbContext _context;
        private Mock<IMapper> _mockMapper;
        private UserAddressService _userAddressService;
        private List<UserAddress> _testAddresses;
        private User _testUser;

        [SetUp]
        public async Task Setup()
        {
            // Create in-memory database
            var options = new DbContextOptionsBuilder<AddressDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AddressDbContext(options);

            // Create test user with all required properties
            _testUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "testuser@example.com", // Add required Email
                Username = "testuser"          // Add required Username
                // Add other required properties if any
            };

            // Add the user to the context first
            await _context.Set<User>().AddAsync(_testUser);
            await _context.SaveChangesAsync();

            // Create test addresses
            _testAddresses = new List<UserAddress>
            {
                new UserAddress(_testUser.Id, _testUser, "123 Main St", null, "New York", "NY", 10001)
                {
                    Id = Guid.NewGuid()
                },
                new UserAddress(_testUser.Id, _testUser, "456 Oak Ave", "Apt 2", "Los Angeles", "CA", 90210)
                {
                    Id = Guid.NewGuid()
                },
                new UserAddress(_testUser.Id, _testUser, "789 Pine Rd", null, "New York", "NY", 10002)
                {
                    Id = Guid.NewGuid()
                }
            };

            // Add test data to in-memory database
            await _context.Set<UserAddress>().AddRangeAsync(_testAddresses);
            await _context.SaveChangesAsync();

            // Setup mapper mock
            _mockMapper = new Mock<IMapper>();
    
            
        }
        [TearDown]
        public void TearDown()
        {
            _context?.Dispose();
        }

        #region GroupByUserStateAsync Tests

        [Test]
        public async Task GroupByUserStateAsync_WithValidData_ReturnsGroupedByState()
        {
            // Act
            var result = await _userAddressService.GroupByUserStateAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2)); // NY and CA
            Assert.That(result.Any(g => g.Key == "NY"), Is.True);
            Assert.That(result.Any(g => g.Key == "CA"), Is.True);
    
            var nyGroup = result.First(g => g.Key == "NY");
            Assert.That(nyGroup.Count(), Is.EqualTo(2));
        }

        [Test]
        public async Task GroupByUserStateAsync_WithEmptyDatabase_ReturnsEmptyGroups()
        {
            // Arrange - Clear the database
            _context.Set<UserAddress>().RemoveRange(_context.Set<UserAddress>());
            await _context.SaveChangesAsync();

            // Act
            var result = await _userAddressService.GroupByUserStateAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task GroupByZipCodeAsync_WithValidData_ReturnsGroupedByZipCode()
        {
            // Act
            var result = await _userAddressService.GroupByZipCodeAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(3)); // 10001, 90210, 10002
            Assert.That(result.Any(g => g.Key == 10001), Is.True);
            Assert.That(result.Any(g => g.Key == 90210), Is.True);
            Assert.That(result.Any(g => g.Key == 10002), Is.True);
        }

        [Test]
        public async Task GroupByZipCodeAsync_WithEmptyDatabase_ReturnsEmptyList()
        {
            // Arrange - Clear the database
            _context.Set<UserAddress>().RemoveRange(_context.Set<UserAddress>());
            await _context.SaveChangesAsync();

            // Act
            var result = await _userAddressService.GroupByZipCodeAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        #endregion

        #region GroupByCityAsync Tests

        [Test]
        public async Task GroupByCityAsync_WithValidData_ReturnsGroupedByCity()
        {
            // Act
            var result = await _userAddressService.GroupByCityAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2)); // New York and Los Angeles
            Assert.That(result.Any(g => g.Key == "New York"), Is.True);
            Assert.That(result.Any(g => g.Key == "Los Angeles"), Is.True);
    
            var nyGroup = result.First(g => g.Key == "New York");
            Assert.That(nyGroup.Count(), Is.EqualTo(2));
        }

        [Test]
        public async Task GroupByCityAsync_WithEmptyDatabase_ReturnsEmptyGroups()
        {
            // Arrange - Clear the database
            _context.Set<UserAddress>().RemoveRange(_context.Set<UserAddress>());
            await _context.SaveChangesAsync();

            // Act
            var result = await _userAddressService.GroupByCityAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        #endregion

        #region GetByUserAddressAsync Tests

        [Test]
        public async Task GetByUserAddressAsync_WithMatchingAddress1_ReturnsUserAddress()
        {
            // Arrange
            var searchAddress = new UserAddress(Guid.NewGuid(), _testUser, "123 Main St", null, "Test City", "TS", 12345);

            // Act
            var result = await _userAddressService.GetByUserAddressAsync(searchAddress);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Address1, Is.EqualTo("123 Main St"));
        }

        [Test]
        public async Task GetByUserAddressAsync_WithMatchingAddress2_ReturnsUserAddress()
        {
            // Arrange
            var searchAddress = new UserAddress(Guid.NewGuid(), _testUser, "Different St", "Apt 2", "Test City", "TS", 12345);

            // Act
            var result = await _userAddressService.GetByUserAddressAsync(searchAddress);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Address2, Is.EqualTo("Apt 2"));
        }

        [Test]
        public void GetByUserAddressAsync_WithNoMatch_ThrowsInvalidOperationException()
        {
            // Arrange
            var searchAddress = new UserAddress(Guid.NewGuid(), _testUser, "Non-existent St", "Non-existent Apt", "Test City", "TS", 12345);

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _userAddressService.GetByUserAddressAsync(searchAddress));
        }

        #endregion

        #region GetAllAsync Tests

        [Test]
        public async Task GetAllAsync_ReturnsAllAddresses()
        {
            // Act
            var result = await _userAddressService.GetAllAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(3));
        }

        [Test]
        public async Task GetAllAsync_WithEmptyDatabase_ReturnsEmptyCollection()
        {
            // Arrange - Clear the database
            _context.Set<UserAddress>().RemoveRange(_context.Set<UserAddress>());
            await _context.SaveChangesAsync();

            // Act
            var result = await _userAddressService.GetAllAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(0));
        }

        #endregion

        #region FindAsync Tests

        [Test]
        public async Task FindAsync_WithValidExpression_ReturnsMatchingAddresses()
        {
            // Act
            var result = await _userAddressService.FindAsync(ua => ua.State == "NY");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(result.All(ua => ua.State == "NY"), Is.True);
        }

        [Test]
        public async Task FindAsync_WithNoMatches_ReturnsEmptyCollection()
        {
            // Act
            var result = await _userAddressService.FindAsync(ua => ua.State == "TX");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task FindAsync_WithComplexExpression_ReturnsMatchingAddresses()
        {
            // Act
            var result = await _userAddressService.FindAsync(ua => ua.City == "New York" && ua.ZipCode > 10001);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(1));
            Assert.That(result.First().ZipCode, Is.EqualTo(10002));
        }

        #endregion

        #region GetByIdAsync Tests

        [Test]
        public async Task GetByIdAsync_WithValidId_ReturnsUserAddress()
        {
            // Arrange
            var addressId = _testAddresses.First().Id;

            // Act
            var result = await _userAddressService.GetByIdAsync(addressId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(addressId));
        }

        [Test]
        public void GetByIdAsync_WithInvalidId_ThrowsInvalidOperationException()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _userAddressService.GetByIdAsync(invalidId));
        }

        [Test]
        public void GetByIdAsync_WithEmptyGuid_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _userAddressService.GetByIdAsync(Guid.Empty));
        }

        #endregion

        #region AddAsync Tests

        [Test]
        public async Task AddAsync_WithValidEntity_AddsEntityAndSavesChanges()
        {
            // Arrange
            var newAddress = new UserAddress(Guid.NewGuid(), _testUser, "999 New St", null, "Chicago", "IL", 60601)
            {
                Id = Guid.NewGuid()
            };

            // Act
            await _userAddressService.AddAsync(newAddress);

            // Assert
            var addedAddress = await _context.Set<UserAddress>().FindAsync(newAddress.Id);
            Assert.That(addedAddress, Is.Not.Null);
            Assert.That(addedAddress.Address1, Is.EqualTo("999 New St"));
            Assert.That(addedAddress.City, Is.EqualTo("Chicago"));
        }

        #endregion

        #region AddRangeAsync Tests

        [Test]
        public async Task AddRangeAsync_WithValidEntities_AddsEntitiesAndSavesChanges()
        {
            // Arrange
            var newAddresses = new List<UserAddress>
            {
                new UserAddress(Guid.NewGuid(), _testUser, "111 First St", null, "Boston", "MA", 02101)
                {
                    Id = Guid.NewGuid()
                },
                new UserAddress(Guid.NewGuid(), _testUser, "222 Second St", null, "Boston", "MA", 02102)
                {
                    Id = Guid.NewGuid()
                }
            };

            // Act
            await _userAddressService.AddRangeAsync(newAddresses);

            // Assert
            var allAddresses = await _context.Set<UserAddress>().ToListAsync();
            Assert.That(allAddresses.Count, Is.EqualTo(5)); // 3 original + 2 new
            
            var bostonAddresses = allAddresses.Where(ua => ua.City == "Boston").ToList();
            Assert.That(bostonAddresses.Count, Is.EqualTo(2));
        }

        #endregion

        #region UpdateAsync Tests

        [Test]
        public async Task UpdateAsync_WithValidEntity_UpdatesEntityAndSavesChanges()
        {
            // Arrange
            var addressToUpdate = _testAddresses.First();
            var originalCity = addressToUpdate.City;
            addressToUpdate.City = "Updated City";

            // Act
            await _userAddressService.UpdateAsync(addressToUpdate);

            // Assert
            var updatedAddress = await _context.Set<UserAddress>().FindAsync(addressToUpdate.Id);
            Assert.That(updatedAddress, Is.Not.Null);
            Assert.That(updatedAddress.City, Is.EqualTo("Updated City"));
            Assert.That(updatedAddress.City, Is.Not.EqualTo(originalCity));
        }

        #endregion

        #region RemoveAsync Tests

        [Test]
        public async Task RemoveAsync_WithValidEntity_RemovesEntityAndSavesChanges()
        {
            // Arrange
            var addressToRemove = _testAddresses.First();
            var addressId = addressToRemove.Id;

            // Act
            await _userAddressService.RemoveAsync(addressToRemove);

            // Assert
            var removedAddress = await _context.Set<UserAddress>().FindAsync(addressId);
            Assert.That(removedAddress, Is.Null);
            
            var remainingAddresses = await _context.Set<UserAddress>().ToListAsync();
            Assert.That(remainingAddresses.Count, Is.EqualTo(2));
        }

        #endregion

        #region UpdateUserAddress Tests

        [Test]
        public async Task UpdateUserAddress_WithValidRequest_UpdatesAndReturnsAddress()
        {
            // Arrange
            var existingAddress = _testAddresses.First();
            var updateRequest = new UserAddressRequest
            {
                Id = existingAddress.Id,
                Address1 = "Updated Address",
                City = "Updated City"
            };

            _mockMapper.Setup(m => m.Map(It.IsAny<UserAddressRequest>(), It.IsAny<UserAddress>()))
                      .Callback<UserAddressRequest, UserAddress>((src, dest) =>
                      {
                          dest.Address1 = src.Address1;
                          dest.City = src.City;
                      });

            // Act
            var result = await _userAddressService.UpdateUserAddress(updateRequest);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(existingAddress.Id));
            _mockMapper.Verify(m => m.Map(updateRequest, It.IsAny<UserAddress>()), Times.Once);
            
            // Verify the address was actually updated in the database
            var updatedAddress = await _context.Set<UserAddress>().FindAsync(existingAddress.Id);
            Assert.That(updatedAddress.Address1, Is.EqualTo("Updated Address"));
            Assert.That(updatedAddress.City, Is.EqualTo("Updated City"));
        }

        [Test]
        public void UpdateUserAddress_WithNonExistentId_ThrowsArgumentNullException()
        {
            // Arrange
            var updateRequest = new UserAddressRequest
            {
                Id = Guid.NewGuid(),
                Address1 = "Some Address"
            };

            // Act & Assert
            var exception = Assert.ThrowsAsync<ArgumentNullException>(
                async () => await _userAddressService.UpdateUserAddress(updateRequest));
            
            Assert.That(exception.ParamName, Is.EqualTo("existingAddress"));
            Assert.That(exception.Message, Does.Contain("User Address does not exist"));
        }

        [Test]
        public void UpdateUserAddress_WithEmptyGuid_ThrowsArgumentNullException()
        {
            // Arrange
            var updateRequest = new UserAddressRequest
            {
                Id = Guid.Empty,
                Address1 = "Some Address"
            };

            // Act & Assert
            var exception = Assert.ThrowsAsync<ArgumentNullException>(
                async () => await _userAddressService.UpdateUserAddress(updateRequest));
            
            Assert.That(exception.ParamName, Is.EqualTo("existingAddress"));
        }

        #endregion

        #region Integration Tests

        [Test]
        public async Task CompleteWorkflow_AddFindUpdateRemove_WorksCorrectly()
        {
            // Arrange
            var newAddress = new UserAddress(Guid.NewGuid(), _testUser, "Integration Test St", null, "Test City", "TC", 12345)
            {
                Id = Guid.NewGuid()
            };

            // Add
            await _userAddressService.AddAsync(newAddress);
            var addedAddress = await _userAddressService.GetByIdAsync(newAddress.Id);
            Assert.That(addedAddress, Is.Not.Null);

            // Find
            var foundAddresses = await _userAddressService.FindAsync(ua => ua.City == "Test City");
            Assert.That(foundAddresses.Count(), Is.EqualTo(1));

            // Update
            addedAddress.City = "Updated Test City";
            await _userAddressService.UpdateAsync(addedAddress);
            var updatedAddress = await _userAddressService.GetByIdAsync(newAddress.Id);
            Assert.That(updatedAddress.City, Is.EqualTo("Updated Test City"));

            // Remove
            await _userAddressService.RemoveAsync(updatedAddress);
            Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _userAddressService.GetByIdAsync(newAddress.Id));
        }

        #endregion
    }
}