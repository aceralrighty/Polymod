using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TBD.AuthModule.Data;
using TBD.AuthModule.Models;

namespace TBD.AuthModule.Controllers;

public class AuthController(AuthDbContext context) : Controller
{
    // GET: Auth
    public async Task<IActionResult> Index()
    {
        return View(await context.AuthUsers.ToListAsync());
    }

    // GET: Auth/Details/5
    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var authUser = await context.AuthUsers
            .FirstOrDefaultAsync(m => m.Id == id);
        if (authUser == null)
        {
            return NotFound();
        }

        return View(authUser);
    }

    // GET: Auth/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Auth/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(
            "AuthId,Username,Email,HashedPassword,RefreshToken,RefreshTokenExpiry,LastLogin,FailedLoginAttempts,Id,CreatedAt,UpdatedAt,DeletedAt")]
        AuthUser authUser)
    {
        if (ModelState.IsValid)
        {
            authUser.Id = Guid.NewGuid();
            context.Add(authUser);
            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        return View(authUser);
    }

    // GET: Auth/Edit/5
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var authUser = await context.AuthUsers.FindAsync(id);
        if (authUser == null)
        {
            return NotFound();
        }

        return View(authUser);
    }

    // POST: Auth/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id,
        [Bind(
            "AuthId,Username,Email,HashedPassword,RefreshToken,RefreshTokenExpiry,LastLogin,FailedLoginAttempts,Id,CreatedAt,UpdatedAt,DeletedAt")]
        AuthUser authUser)
    {
        if (id != authUser.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                context.Update(authUser);
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AuthUserExists(authUser.Id))
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

        return View(authUser);
    }

    // GET: Auth/Delete/5
    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var authUser = await context.AuthUsers
            .FirstOrDefaultAsync(m => m.Id == id);
        if (authUser == null)
        {
            return NotFound();
        }

        return View(authUser);
    }

    // POST: Auth/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var authUser = await context.AuthUsers.FindAsync(id);
        if (authUser != null)
        {
            context.AuthUsers.Remove(authUser);
        }

        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool AuthUserExists(Guid id)
    {
        return context.AuthUsers.Any(e => e.Id == id);
    }
}
