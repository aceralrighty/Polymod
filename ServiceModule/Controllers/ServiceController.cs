using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TBD.ServiceModule.Data;
using TBD.ServiceModule.Models;

namespace TBD.ServiceModule.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ServiceController(ServiceDbContext context) : ControllerBase
{
    // GET: api/Service
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Service>>> GetServices()
    {
        return await context.Services.ToListAsync();
    }

    // GET: api/Service/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Service>> GetService(Guid id)
    {
        var service = await context.Services.FindAsync(id);

        if (service == null)
        {
            return NotFound();
        }

        return service;
    }

    // PUT: api/Service/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutService(Guid id, Service service)
    {
        if (id != service.Id)
        {
            return BadRequest();
        }

        context.Entry(service).State = EntityState.Modified;

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ServiceExists(id))
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

    // POST: api/Service
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<Service>> PostService(Service service)
    {
        context.Services.Add(service);
        await context.SaveChangesAsync();

        return CreatedAtAction("GetService", new { id = service.Id }, service);
    }

    // DELETE: api/Service/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteService(Guid id)
    {
        var service = await context.Services.FindAsync(id);
        if (service == null)
        {
            return NotFound();
        }

        context.Services.Remove(service);
        await context.SaveChangesAsync();

        return NoContent();
    }

    private bool ServiceExists(Guid id)
    {
        return context.Services.Any(e => e.Id == id);
    }
}