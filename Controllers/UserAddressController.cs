using Microsoft.AspNetCore.Mvc;
using TBD.Data.Seeding;
using TBD.Models.DTOs;
using TBD.Models.Entities;
using TBD.Repository.UserAddress;

namespace TBD.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserAddressController(
    IUserAddressService userAddressService,
    IUserAddressRepository userAddressRepository)
    : ControllerBase
{
    // GET: api/UserAddress
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserAddress>>> GetAll()
    {
        var addresses = await userAddressService.GetAllAsync();
        return Ok(addresses);
    }

    // GET: api/UserAddress/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<UserAddress>> GetById(Guid id)
    {
        try
        {
            var address = await userAddressService.GetByIdAsync(id);
            return Ok(address);
        }
        catch (InvalidOperationException)
        {
            return NotFound($"Address with ID {id} not found");
        }
    }

    // POST: api/UserAddress/seed
    [HttpPost("seed")]
    public async Task<ActionResult> SeedData([FromServices] IServiceProvider serviceProvider)
    {
        try
        {
            await DataSeeder.SeedAsync(serviceProvider);
            return Ok("Data seeded successfully");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error seeding data: {ex.Message}");
        }
    }

    // PUT: api/UserAddress/update
    [HttpPut("update")]
    public async Task<ActionResult<UserAddress>> UpdateAddress([FromBody] UserAddressRequest request)
    {
        
        try
        {
            var updatedAddress = await userAddressRepository.UpdateUserAddress(request);
            return Ok(updatedAddress);
        }
        catch (ArgumentNullException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error updating address: {ex.Message}");
        }
    }

    // GET: api/UserAddress/test
    [HttpGet("test")]
    public async Task<ActionResult> TestUpdateFlow()
    {
        try
        {
            // Get all addresses
            var addresses = await userAddressService.GetAllAsync();
            var addressToUpdate = addresses.FirstOrDefault();

            if (addressToUpdate == null)
            {
                return NotFound("No addresses found to test update functionality. Please seed data first.");
            }

            // Create original address snapshot for comparison
            var originalAddress = new
            {
                addressToUpdate.Id,
                addressToUpdate.Address1,
                addressToUpdate.Address2,
                addressToUpdate.City,
                addressToUpdate.State,
                addressToUpdate.ZipCode
            };

            // Create update request with some changed fields
            var updateRequest = new UserAddressRequest(
                id: addressToUpdate.Id,
                address1: "Updated Test Address",
                address2: addressToUpdate.Address2, // Keep the same
                city: "Updated City",
                state: addressToUpdate.State, // Keep the same
                zipCode: (int)addressToUpdate.ZipCode + 1000 // Change zip code
            );

            // Update the address
            var updatedAddress = await userAddressRepository.UpdateUserAddress(updateRequest);

            // Return before/after comparison
            return Ok(new
            {
                message = "Address updated successfully",
                original = originalAddress,
                updated = new
                {
                    updatedAddress.Id,
                    updatedAddress.Address1,
                    updatedAddress.Address2,
                    updatedAddress.City,
                    updatedAddress.State,
                    updatedAddress.ZipCode
                },
                changes = new
                {
                    Address1Changed = originalAddress.Address1 != updatedAddress.Address1,
                    Address2Changed = originalAddress.Address2 != updatedAddress.Address2,
                    CityChanged = originalAddress.City != updatedAddress.City,
                    StateChanged = originalAddress.State != updatedAddress.State,
                    ZipCodeChanged = originalAddress.ZipCode != updatedAddress.ZipCode
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error testing update flow: {ex.Message}");
        }
    }
}