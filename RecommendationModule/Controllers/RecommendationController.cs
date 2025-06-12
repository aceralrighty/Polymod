using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TBD.RecommendationModule.Data;
using TBD.RecommendationModule.Models;

namespace TBD.RecommendationModule.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RecommendationController(RecommendationDbContext context) : ControllerBase
{
    // GET: api/Recommendation
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserRecommendation>>> GetUserRecommendations()
    {
        return await context.UserRecommendations.ToListAsync();
    }

    // GET: api/Recommendation/5
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserRecommendation>> GetUserRecommendation(Guid id)
    {
        var userRecommendation = await context.UserRecommendations.FindAsync(id);

        if (userRecommendation == null)
        {
            return NotFound();
        }

        return userRecommendation;
    }

    // PUT: api/Recommendation/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> PutUserRecommendation(Guid id, UserRecommendation userRecommendation)
    {
        if (id != userRecommendation.Id)
        {
            return BadRequest();
        }

        context.Entry(userRecommendation).State = EntityState.Modified;

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!UserRecommendationExists(id))
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

    // POST: api/Recommendation
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<UserRecommendation>> PostUserRecommendation(
        UserRecommendation userRecommendation)
    {
        context.UserRecommendations.Add(userRecommendation);
        await context.SaveChangesAsync();

        return CreatedAtAction("GetUserRecommendation", new { id = userRecommendation.Id }, userRecommendation);
    }

    // DELETE: api/Recommendation/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUserRecommendation(Guid id)
    {
        var userRecommendation = await context.UserRecommendations.FindAsync(id);
        if (userRecommendation == null)
        {
            return NotFound();
        }

        context.UserRecommendations.Remove(userRecommendation);
        await context.SaveChangesAsync();

        return NoContent();
    }

    private bool UserRecommendationExists(Guid id)
    {
        return context.UserRecommendations.Any(e => e.Id == id);
    }
}
