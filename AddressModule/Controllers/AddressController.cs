using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TBD.AddressModule.Data;
using TBD.AddressModule.Models;

namespace TBD.AddressModule.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AddressController : ControllerBase
    {
        private readonly AddressDbContext _context;

        public AddressController(AddressDbContext context)
        {
            _context = context;
        }

        // GET: api/Address
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserAddress>>> GetUserAddress()
        {
            return await _context.UserAddress.ToListAsync();
        }

        // GET: api/Address/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserAddress>> GetUserAddress(Guid id)
        {
            var userAddress = await _context.UserAddress.FindAsync(id);

            if (userAddress == null)
            {
                return NotFound();
            }

            return userAddress;
        }

        // PUT: api/Address/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUserAddress(Guid id, UserAddress userAddress)
        {
            if (id != userAddress.Id)
            {
                return BadRequest();
            }

            _context.Entry(userAddress).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserAddressExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Address
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<UserAddress>> PostUserAddress(UserAddress userAddress)
        {
            _context.UserAddress.Add(userAddress);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUserAddress", new { id = userAddress.Id }, userAddress);
        }

        // DELETE: api/Address/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserAddress(Guid id)
        {
            var userAddress = await _context.UserAddress.FindAsync(id);
            if (userAddress == null)
            {
                return NotFound();
            }

            _context.UserAddress.Remove(userAddress);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserAddressExists(Guid id)
        {
            return _context.UserAddress.Any(e => e.Id == id);
        }
    }
}
