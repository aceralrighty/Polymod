using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TBD.AddressModule.Data;
using TBD.AddressModule.Models;
using TBD.AddressModule.Services;
using TBD.API.DTOs;

namespace TBD.AddressModule.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AddressController(IUserAddressService userAddressService, IMapper mapper, AddressDbContext context)
        : ControllerBase
    {
        // GET: api/Address
        [Route("api/[controller]/{id}")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserAddressResponse>>> GetUserAddresses(Guid id,
            [FromBody] UserAddressRequest request)
        {
            var addresses = await userAddressService.GetAllAsync(id);
            var result = mapper.Map<IEnumerable<UserAddressResponse>>(addresses);
            return Ok(result);
        }

        // GET: api/Address/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserAddressResponse>> GetUserAddress(Guid id)
        {
            var address = await userAddressService.GetByIdAsync(id);
            if (address == null)
            {
                return NotFound();
            }

            var result = mapper.Map<UserAddressResponse>(address);
            return Ok(result);
        }

        // POST: api/Address
        [HttpPost]
        public async Task<ActionResult<UserAddressResponse>> PostUserAddress([FromBody] UserAddressRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entity = mapper.Map<UserAddress>(request);
            await userAddressService.AddAsync(entity);

            var responseDto = mapper.Map<UserAddressResponse>(entity);
            return CreatedAtAction(nameof(GetUserAddress), new { id = entity.Id }, responseDto);
        }

        // PUT: api/Address/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUserAddress(Guid id, [FromBody] UserAddressRequest request)
        {
            if (id != request.Id)
                return BadRequest("ID in URL and body must match");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingAddress = await userAddressService.GetByIdAsync(id);
            if (existingAddress == null)
                return NotFound();

            // Map the updated values from DTO to existing entity
            mapper.Map(request, existingAddress);
            await userAddressService.UpdateAsync(existingAddress);

            return NoContent();
        }

        // DELETE: api/Address/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserAddress(Guid id)
        {
            var address = await context.UserAddress.Where(u => u.DeletedAt == null)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (address == null)
            {
                return NotFound();
            }

            // Soft delete: set DeletedAt timestamp instead of removing
            address.DeletedAt = DateTime.UtcNow;
            address.UpdatedAt = DateTime.UtcNow;

            context.Entry(address).State = EntityState.Modified;
            await context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}/permanent")]
        public async Task<IActionResult> DeleteUserAddressPermanently(Guid id)
        {
            var address = await context.UserAddress.FindAsync(id);
            if (address == null)
            {
                return NotFound();
            }

            context.UserAddress.Remove(address);
            await context.SaveChangesAsync();
            return NoContent();
        }
    }
}