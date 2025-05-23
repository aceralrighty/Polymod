using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using TBD.AddressModule.Models;
using TBD.AddressModule.Services;
using TBD.API.DTOs;

namespace TBD.AddressModule.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AddressController(IUserAddressService userAddressService, IMapper mapper) : ControllerBase
    {
        // GET: api/Address
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserAddressResponse>>> GetUserAddresses()
        {
            var addresses = await userAddressService.GetAllAsync();
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
            var existingAddress = await userAddressService.GetByIdAsync(id);
            if (existingAddress == null)
                return NotFound();

            await userAddressService.RemoveAsync(existingAddress);
            return NoContent();
        }
    }
}