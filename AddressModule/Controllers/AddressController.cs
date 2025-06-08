using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TBD.AddressModule.Data;
using TBD.AddressModule.Models;
using TBD.UserModule.Data;
using TBD.UserModule.Models;

namespace TBD.AddressModule.Controllers;

public class AddressController(AddressDbContext context, UserDbContext userContext) : Controller
{
    private readonly UserDbContext _userContext = userContext;

    // GET: Address
    public async Task<IActionResult> Index()
    {
        var addressDbContext = context.UserAddress.Include(u => u.User);
        return View(await addressDbContext.ToListAsync());
    }

    // GET: Address/Details/5
    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userAddress = await context.UserAddress
            .Include(u => u.User)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (userAddress == null)
        {
            return NotFound();
        }

        return View(userAddress);
    }

    // GET: Address/Create
    public IActionResult Create()
    {
        ViewData["UserId"] = new SelectList(context.Set<User>(), "Id", "Id");
        return View();
    }

    // POST: Address/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("UserId,Address1,Address2,City,State,ZipCode,Id,CreatedAt,UpdatedAt,DeletedAt")]
        UserAddress userAddress)
    {
        if (ModelState.IsValid)
        {
            userAddress.Id = Guid.NewGuid();
            context.Add(userAddress);
            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        ViewData["UserId"] = new SelectList(context.Set<User>(), "Id", "Id", userAddress.UserId);
        return View(userAddress);
    }

    // GET: Address/Edit/5
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userAddress = await context.UserAddress.FindAsync(id);
        if (userAddress == null)
        {
            return NotFound();
        }

        ViewData["UserId"] = new SelectList(context.Set<User>(), "Id", "Id", userAddress.UserId);
        return View(userAddress);
    }

    // POST: Address/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id,
        [Bind("UserId,Address1,Address2,City,State,ZipCode,Id,CreatedAt,UpdatedAt,DeletedAt")]
        UserAddress userAddress)
    {
        if (id != userAddress.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                context.Update(userAddress);
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserAddressExists(userAddress.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToAction(nameof(Index));
        }

        ViewData["UserId"] = new SelectList(context.Set<User>(), "Id", "Id", userAddress.UserId);
        return View(userAddress);
    }

    // GET: Address/Delete/5
    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userAddress = await context.UserAddress
            .Include(u => u.User)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (userAddress == null)
        {
            return NotFound();
        }

        return View(userAddress);
    }

    // POST: Address/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var userAddress = await context.UserAddress.FindAsync(id);
        if (userAddress != null)
        {
            context.UserAddress.Remove(userAddress);
        }

        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool UserAddressExists(Guid id)
    {
        return context.UserAddress.Any(e => e.Id == id);
    }
}
