using Microsoft.AspNetCore.Mvc;
using TBD.API.DTOs;
using TBD.API.Interfaces;

namespace TBD.UserModule.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController(IUserService userService) : ControllerBase
{
    // GET: api/User - PAGINATED to avoid LOH issues
    [HttpGet]
    public async Task<ActionResult<PagedResult<UserDto>>> GetUsers(int page = 1, int pageSize = 50)
    {
        try
        {
            var result = await userService.GetUsersAsync(page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // GET: api/User/5
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(Guid id)
    {
        var user = await userService.GetUserByIdAsync(id);

        if (user == null)
        {
            return NotFound($"User with ID {id} not found");
        }

        return Ok(user);
    }

    // GET: api/User/email/{email}
    [HttpGet("email/{email}")]
    public async Task<ActionResult<UserDto>> GetUserByEmail(string email)
    {
        var user = await userService.GetUserByEmailAsync(email);

        if (user == null)
        {
            return NotFound($"User with email {email} not found");
        }

        return Ok(user);
    }

    // GET: api/User/username/{username}
    [HttpGet("username/{username}")]
    public async Task<ActionResult<UserDto>> GetUserByUsername(string username)
    {
        var user = await userService.GetUserByUsernameAsync(username);

        if (user == null)
        {
            return NotFound($"User with username {username} not found");
        }

        return Ok(user);
    }

    // PUT: api/User/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutUser(Guid id, UserDto userDto)
    {
        if (id != userDto.Id)
        {
            return BadRequest("ID in URL doesn't match ID in request body");
        }

        try
        {
            await userService.UpdateUserAsync(userDto);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // POST: api/User
    [HttpPost]
    public async Task<ActionResult<UserDto>> PostUser(UserDto userDto)
    {
        try
        {
            await userService.CreateUserAsync(userDto);

            // Return the created user (fetch it to get the generated ID)
            var createdUser = await userService.GetUserByEmailAsync(userDto.Email ?? string.Empty);
            return CreatedAtAction("GetUser", new { id = createdUser?.Id }, createdUser);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // DELETE: api/User/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        try
        {
            await userService.DeleteUserAsync(id);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}
