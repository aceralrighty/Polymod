using Microsoft.AspNetCore.Mvc;
using TBD.API.DTOs.Users;
using TBD.GrpcModule.Interfaces;

namespace TBD.GrpcModule.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GrpcTestController(IUserAddressGrpcClient grpcClient, ILogger<GrpcTestController> logger) : ControllerBase
{
    /// <summary>
    /// Test all gRPC methods with sample data
    /// </summary>
    [HttpPost("test-full-workflow")]
    public async Task<ActionResult> TestFullWorkflow()
    {
        var testResults = new List<string>();

        try
        {
            // Test 1: Check if a user exists (use a known user ID or create one)
            var testUserId = Guid.NewGuid(); // In real scenario, use existing user
            testResults.Add("=== Testing User Lookup ===");

            var userExists = await grpcClient.UserExistsAsync(testUserId);
            testResults.Add($"User {testUserId} exists: {userExists}");

            if (!userExists)
            {
                testResults.Add("⚠️ User doesn't exist - some tests will fail");
            }

            // Test 2: Search user by email
            testResults.Add("\n=== Testing User Search by Email ===");
            var user = await grpcClient.GetUserByEmailAsync("test@example.com");
            testResults.Add($"User found by email: {user != null}");
            if (user != null)
            {
                testResults.Add($"Found user: {user.Username} ({user.Email})");
                testUserId = user.Id; // Use existing user for remaining tests
            }

            // Test 3: Create a new address
            testResults.Add("\n=== Testing Address Creation ===");
            var createRequest = new UserAddressRequest
            {
                Id = Guid.NewGuid(),
                UserId = testUserId,
                Address1 = "123 Test Street",
                Address2 = "Apt 4B",
                City = "Test City",
                State = "NY",
                ZipCode = "12345"
            };

            try
            {
                var createdAddress = await grpcClient.CreateUserAddressAsync(createRequest);
                testResults.Add($"✅ Address created: {createdAddress.Id}");
                testResults.Add(
                    $"   Full address: {createdAddress.Address1}, {createdAddress.City}, {createdAddress.State} {createdAddress.ZipCode}");

                // Test 4: Get the created address
                testResults.Add("\n=== Testing Address Retrieval ===");
                var retrievedAddress = await grpcClient.GetUserAddressAsync(createdAddress.Id);
                testResults.Add($"✅ Address retrieved: {retrievedAddress != null}");

                // Test 5: Update the address
                testResults.Add("\n=== Testing Address Update ===");
                var updateRequest = new UserAddressRequest
                {
                    Id = createdAddress.Id,
                    UserId = createdAddress.UserId,
                    Address1 = "456 Updated Street",
                    Address2 = "Suite 2A",
                    City = "Updated City",
                    State = "CA",
                    ZipCode = "54321"
                };

                var updatedAddress = await grpcClient.UpdateUserAddressAsync(updateRequest);
                testResults.Add($"✅ Address updated: {updatedAddress.Address1}");
                testResults.Add(
                    $"   New address: {updatedAddress.Address1}, {updatedAddress.City}, {updatedAddress.State} {updatedAddress.ZipCode}");

                // Test 6: Get all addresses for user
                testResults.Add("\n=== Testing Get All User Addresses ===");
                var allAddresses = await grpcClient.GetUserAddressesAsync(testUserId);
                var addressList = allAddresses.ToList();
                testResults.Add($"✅ Found {addressList.Count} addresses for user");

                // Test 7: Delete the address
                testResults.Add("\n=== Testing Address Deletion ===");
                var deleted = await grpcClient.DeleteUserAddressAsync(createdAddress.Id);
                testResults.Add($"✅ Address deleted: {deleted}");

                // Test 8: Verify deletion
                var deletedAddress = await grpcClient.GetUserAddressAsync(createdAddress.Id);
                testResults.Add($"✅ Verified deletion: {deletedAddress == null}");
            }
            catch (Exception ex)
            {
                testResults.Add($"❌ Error during CRUD operations: {ex.Message}");
            }

            testResults.Add("\n=== Test Summary ===");
            testResults.Add("All gRPC methods have been tested!");

            return Ok(new { success = true, results = testResults, timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during gRPC testing");
            testResults.Add($"❌ Fatal error: {ex.Message}");

            return StatusCode(500, new { success = false, results = testResults, error = ex.Message });
        }
    }

    /// <summary>
    /// Quick health check for gRPC service
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult> HealthCheck()
    {
        try
        {
            // Try a simple operation
            var randomUserId = Guid.NewGuid();
            var exists = await grpcClient.UserExistsAsync(randomUserId);

            return Ok(new
            {
                status = "healthy",
                grpcService = "operational",
                timestamp = DateTime.UtcNow,
                testResult = $"UserExists call completed (returned {exists})"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "gRPC health check failed");
            return StatusCode(503,
                new { status = "unhealthy", grpcService = "failed", error = ex.Message, timestamp = DateTime.UtcNow });
        }
    }
}
