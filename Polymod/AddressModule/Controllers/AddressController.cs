using Microsoft.AspNetCore.Mvc;
using PolyMod.API.DTOs.Users;
using PolyMod.GrpcModule.Interfaces;

namespace PolyMod.AddressModule.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserAddressController(IUserAddressGrpcClient grpcClient, ILogger<UserAddressController> logger) : ControllerBase
{
    /// <summary>
    /// Get all addresses for a specific user
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<IEnumerable<UserAddressResponse>>> GetUserAddresses(Guid userId)
    {
        try
        {
            // First check if user exists via gRPC
            var userExists = await grpcClient.UserExistsAsync(userId);
            if (!userExists)
            {
                return NotFound($"User with ID {userId} not found");
            }

            var addresses = await grpcClient.GetUserAddressesAsync(userId);
            return Ok(addresses);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving addresses for user {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving addresses");
        }
    }

    /// <summary>
    /// Get a specific address by ID
    /// </summary>
    [HttpGet("{addressId:guid}")]
    public async Task<ActionResult<UserAddressResponse>> GetAddress(Guid addressId)
    {
        try
        {
            var address = await grpcClient.GetUserAddressAsync(addressId);
            if (address == null)
            {
                return NotFound($"Address with ID {addressId} not found");
            }

            return Ok(address);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving address {AddressId}", addressId);
            return StatusCode(500, "An error occurred while retrieving the address");
        }
    }

    /// <summary>
    /// Create a new address for a user
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserAddressResponse>> CreateAddress([FromBody] UserAddressRequest request)
    {
        try
        {
            // Validate user exists
            var userExists = await grpcClient.UserExistsAsync(request.UserId);
            if (!userExists)
            {
                return BadRequest($"User with ID {request.UserId} not found");
            }

            var createdAddress = await grpcClient.CreateUserAddressAsync(request);
            return CreatedAtAction(nameof(GetAddress), new { addressId = createdAddress.Id }, createdAddress);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating address for user {UserId}", request.UserId);
            return StatusCode(500, "An error occurred while creating the address");
        }
    }

    /// <summary>
    /// Update an existing address
    /// </summary>
    [HttpPut("{addressId:guid}")]
    public async Task<ActionResult<UserAddressResponse>> UpdateAddress(Guid addressId, [FromBody] UserAddressRequest request)
    {
        try
        {
            if (addressId != request.Id)
            {
                return BadRequest("Address ID in URL does not match request body");
            }

            var updatedAddress = await grpcClient.UpdateUserAddressAsync(request);
            return Ok(updatedAddress);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating address {AddressId}", addressId);
            return StatusCode(500, "An error occurred while updating the address");
        }
    }

    /// <summary>
    /// Delete an address
    /// </summary>
    [HttpDelete("{addressId:guid}")]
    public async Task<ActionResult> DeleteAddress(Guid addressId)
    {
        try
        {
            var deleted = await grpcClient.DeleteUserAddressAsync(addressId);
            if (!deleted)
            {
                return NotFound($"Address with ID {addressId} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting address {AddressId}", addressId);
            return StatusCode(500, "An error occurred while deleting the address");
        }
    }

    /// <summary>
    /// Search users by email (demonstrates user lookup via gRPC)
    /// </summary>
    [HttpGet("search/user")]
    public async Task<ActionResult<UserDto>> SearchUserByEmail([FromQuery] string email)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest("Email parameter is required");
            }

            var user = await grpcClient.GetUserByEmailAsync(email);
            if (user == null)
            {
                return NotFound($"User with email {email} not found");
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching for user with email {Email}", email);
            return StatusCode(500, "An error occurred while searching for the user");
        }
    }
}
