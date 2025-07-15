using Microsoft.AspNetCore.Mvc;
using TBD.API.DTOs;
using TBD.API.DTOs.Users;
using TBD.MetricsModule.OpenTelemetry.Services;
using TBD.MetricsModule.Services.Interfaces;
using TBD.UserModule.Services;

namespace TBD.UserModule.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController(IUserService userService, IMetricsServiceFactory metricsServiceFactory) : ControllerBase
{
    private readonly IMetricsService _userMetrics = metricsServiceFactory.CreateMetricsService("UserModule");

    // GET: api/User - PAGINATED to avoid LOH issues
    [HttpGet]
    public async Task<ActionResult<PagedResult<UserDto>>> GetUsers(int page = 1, int pageSize = 50)
    {
        try
        {
            var result = await userService.GetUsersAsync(page, pageSize);
            _userMetrics.IncrementCounter("users_get_requests_total");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _userMetrics.IncrementCounter("users_get_errors_total");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // GET: api/User/5
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(Guid id)
    {
        try
        {
            var user = await userService.GetUserByIdAsync(id);

            if (user == null)
            {
                _userMetrics.IncrementCounter("users_get_by_id_not_found_total");
                return NotFound($"User with ID {id} not found");
            }

            _userMetrics.IncrementCounter("users_get_by_id_success_total");
            return Ok(user);
        }
        catch (Exception ex)
        {
            _userMetrics.IncrementCounter("users_get_by_id_errors_total");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // GET: api/User/email/{email}
    [HttpGet("email/{email}")]
    public async Task<ActionResult<UserDto>> GetUserByEmail(string email)
    {
        try
        {
            var user = await userService.GetUserByEmailAsync(email);

            if (user == null)
            {
                _userMetrics.IncrementCounter("users_get_by_email_not_found_total");
                return NotFound($"User with email {email} not found");
            }

            _userMetrics.IncrementCounter("users_get_by_email_success_total");
            return Ok(user);
        }
        catch (Exception ex)
        {
            _userMetrics.IncrementCounter("users_get_by_email_errors_total");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // GET: api/User/username/{username}
    [HttpGet("username/{username}")]
    public async Task<ActionResult<UserDto>> GetUserByUsername(string username)
    {
        try
        {
            var user = await userService.GetUserByUsernameAsync(username);

            if (user == null)
            {
                _userMetrics.IncrementCounter("users_get_by_username_not_found_total");
                return NotFound($"User with username {username} not found");
            }

            _userMetrics.IncrementCounter("users_get_by_username_success_total");
            return Ok(user);
        }
        catch (Exception ex)
        {
            _userMetrics.IncrementCounter("users_get_by_username_errors_total");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // PUT: api/User/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutUser(Guid id, UserDto? userDto)
    {
        if (id != userDto.Id)
        {
            return BadRequest("ID in URL doesn't match ID in request body");
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await userService.UpdateUserAsync(userDto);

            _userMetrics.IncrementCounter("users_updated_total");

            // Record histogram using OpenTelemetry-specific service
            if (_userMetrics is OpenTelemetryMetricsService openTelemetryService)
            {
                openTelemetryService.RecordHistogram("user_update_duration_ms", stopwatch.ElapsedMilliseconds);
            }

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            _userMetrics.IncrementCounter("users_update_errors_total");
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _userMetrics.IncrementCounter("users_update_errors_total");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // POST: api/User
    [HttpPost]
    public async Task<ActionResult<UserDto>> PostUser(UserDto? userDto)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await userService.CreateUserAsync(userDto);
            var createdUser = await userService.GetUserByEmailAsync(userDto?.Email ?? string.Empty);

            _userMetrics.IncrementCounter("users_created_total");

            // Record histogram using OpenTelemetry-specific service
            if (_userMetrics is OpenTelemetryMetricsService openTelemetryService)
            {
                openTelemetryService.RecordHistogram("user_creation_duration_ms", stopwatch.ElapsedMilliseconds);
            }

            return CreatedAtAction("GetUser", new { id = createdUser?.Id }, createdUser);
        }
        catch (ArgumentException ex)
        {
            _userMetrics.IncrementCounter("users_creation_errors_total");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _userMetrics.IncrementCounter("users_creation_errors_total");
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
            _userMetrics.IncrementCounter("users_deleted_total");
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            _userMetrics.IncrementCounter("users_deletion_errors_total");
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _userMetrics.IncrementCounter("users_deletion_errors_total");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}

