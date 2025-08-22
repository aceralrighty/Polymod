using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockPredictionService.Context;
using StockPredictionService.Models.Stocks;

namespace StockPredictionService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class StockController(StockDbContext context) : ControllerBase
{
    // GET: api/Stock
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Stock>>> GetStocks(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        // Limit pageSize to prevent abuse
        pageSize = Math.Min(pageSize, 5000);

        var stocks = await context.Stocks
            .OrderBy(s => s.Id) // Always order for consistent pagination
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking() // Better performance for read-only operations
            .ToListAsync().ConfigureAwait(false);

        return stocks;
    }

    // GET: api/Stock/recent - Get most recent stocks
    [HttpGet("recent")]
    public async Task<ActionResult<IEnumerable<Stock>>> GetRecentStocks([FromQuery] int count = 1000)
    {
        count = Math.Min(count, 5000); // Limit to prevent memory issues

        var stocks = await context.Stocks
            .OrderByDescending(s => s.Id) // Assuming newer records have higher IDs
            // Or use: .OrderByDescending(s => s.CreatedDate) if you have a date field
            .Take(count)
            .AsNoTracking()
            .ToListAsync().ConfigureAwait(false);

        return stocks;
    }

    // GET: api/Stock/sample - Get a random sample
    [HttpGet("sample")]
    public async Task<ActionResult<IEnumerable<Stock>>> GetSampleStocks([FromQuery] int count = 1000)
    {
        count = Math.Min(count, 5000);

        // Simple random sampling - not perfectly random but fast
        var totalCount = await context.Stocks.CountAsync().ConfigureAwait(false);
        var skip = Random.Shared.Next(0, Math.Max(1, totalCount - count));

        var stocks = await context.Stocks
            .OrderBy(s => s.Id)
            .Skip(skip)
            .Take(count)
            .AsNoTracking()
            .ToListAsync().ConfigureAwait(false);

        return stocks;
    }

    // GET: api/Stock/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Stock>> GetStock(Guid id)
    {
        var stock = await context.Stocks
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id).ConfigureAwait(false);

        if (stock == null)
        {
            return NotFound();
        }

        return stock;
    }

    // GET: api/Stock/search - Search with filtering
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<Stock>>> SearchStocks(
        [FromQuery] string? symbol = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        pageSize = Math.Min(pageSize, 1000);

        var query = context.Stocks.AsQueryable();

        if (!string.IsNullOrEmpty(symbol))
        {
            query = query.Where(s => s.Symbol.Contains(symbol));
        }

        var stocks = await query
            .OrderBy(s => s.Symbol)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync().ConfigureAwait(false);

        return stocks;
    }

    // PUT: api/Stock/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutStock(Guid id, Stock stock)
    {
        if (id != stock.Id)
        {
            return BadRequest();
        }

        context.Entry(stock).State = EntityState.Modified;

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!StockExists(id))
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

    // POST: api/Stock
    [HttpPost]
    public async Task<ActionResult<Stock>> PostStock(Stock stock)
    {
        context.Stocks.Add(stock);
        await context.SaveChangesAsync();

        return CreatedAtAction("GetStock", new { id = stock.Id }, stock);
    }

    // DELETE: api/Stock/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteStock(Guid id)
    {
        var stock = await context.Stocks.FindAsync(id);
        if (stock == null)
        {
            return NotFound();
        }

        context.Stocks.Remove(stock);
        await context.SaveChangesAsync();

        return NoContent();
    }

    private bool StockExists(Guid id)
    {
        return context.Stocks.Any(e => e.Id == id);
    }
}
