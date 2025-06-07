using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TBD.AuthModule.Data;
using TBD.AuthModule.Models;

namespace TBD.AuthModule.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(AuthDbContext context) : ControllerBase
{
    // GET: api/Auth
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuthUser>>> GetAuthUsers()
    {
        return await context.AuthUsers.ToListAsync();
    }

    // GET: api/Auth/5
    [HttpGet("{id}")]
    public async Task<ActionResult<AuthUser>> GetAuthUser(Guid id)
    {
        var authUser = await context.AuthUsers.FindAsync(id);

        if (authUser == null)
        {
            return NotFound();
        }

        return authUser;
    }

    // PUT: api/Auth/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutAuthUser(Guid id, AuthUser authUser)
    {
        if (id != authUser.Id)
        {
            return BadRequest();
        }

        context.Entry(authUser).State = EntityState.Modified;

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!AuthUserExists(id))
            {
                return NotFound();
            }

            throw;
        }

        return NoContent();
    }

    // POST: api/Auth
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<AuthUser>> PostAuthUser(AuthUser authUser)
    {
        context.AuthUsers.Add(authUser);
        await context.SaveChangesAsync();

        return CreatedAtAction("GetAuthUser", new { id = authUser.Id }, authUser);
    }

    // DELETE: api/Auth/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAuthUser(Guid id)
    {
        var authUser = await context.AuthUsers.FindAsync(id);
        if (authUser == null)
        {
            return NotFound();
        }

        context.AuthUsers.Remove(authUser);
        await context.SaveChangesAsync();

        return NoContent();
    }

    private bool AuthUserExists(Guid id)
    {
        return context.AuthUsers.Any(e => e.Id == id);
    }
}
