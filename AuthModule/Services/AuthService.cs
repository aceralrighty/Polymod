using Microsoft.EntityFrameworkCore;
using TBD.API.DTOs.AuthDTO;
using TBD.AuthModule.Data;
using TBD.AuthModule.Exceptions;
using TBD.AuthModule.Models;
using TBD.AuthModule.Repositories;
using TBD.Shared.Utils;

namespace TBD.AuthModule.Services;

internal class AuthService : IAuthService, IAuthRepository
{
    private readonly AuthDbContext _dbContext;
    private readonly DbSet<AuthUser> _dbSet;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly IHasher _hasher;

    public AuthService(AuthDbContext dbContext, IConfiguration configuration, ILogger<AuthService> logger,
        IHasher hasher)
    {
        _dbContext = dbContext;
        _dbSet = _dbContext.Set<AuthUser>();
        _configuration = configuration;
        _logger = logger;
        _hasher = hasher;
    }

    public async Task<AuthUser?> AuthenticateAsync(string username, string password)
    {
        var authUser = await GetAuthUserByUsernameAsync(username);
        if (authUser == null) return null;
        if (_hasher.Verify(authUser.HashedPassword, password))
        {
            authUser.LastLogin = DateTime.UtcNow;
            authUser.FailedLoginAttempts = 0;
            await _dbContext.SaveChangesAsync();
            return authUser;
        }

        authUser.FailedLoginAttempts++;
        await _dbContext.SaveChangesAsync();
        return null;
    }

    public async Task<AuthUser?> GetAuthUserByUsernameAsync(string username)
    {
        return await _dbSet.FirstOrDefaultAsync(au => au.Username == username);
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return new AuthResponse { isSuccessful = false, Message = $"All fields are required to be filled in" };
            }

            var existingUser = await _dbSet.FirstOrDefaultAsync(au => au.Username == request.Username);
            if (existingUser != null)
            {
                return new AuthResponse
                {
                    isSuccessful = false, Message = $"Username {request.Username} already exists"
                };
            }

            // just a random small length for testing
            if (request.Password.Length < 9)
            {
                return new AuthResponse { isSuccessful = false, Message = $"Password must be at least 9 characters" };
            }

            var createNewUser = new AuthUser
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                HashedPassword = _hasher.HashPassword(request.Password),
                FailedLoginAttempts = 0,
            };
            await _dbSet.AddAsync(createNewUser);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("User {Username} created", createNewUser.Username);
            return new AuthResponse
            {
                isSuccessful = true,
                Message = "Registration successful",
                Username = createNewUser.Username,
                Token = createNewUser.RefreshToken!
            };
        }
        catch (ErrorDuringUserRegistrationException ex)
        {
            _logger.LogError("Error during registration: {ExMessage}", ex.Message);
            return new AuthResponse { isSuccessful = false, Message = ex.Message };
        }
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return new AuthResponse { isSuccessful = false, Message = "Username and password are required" };
            }

            var authUser = await AuthenticateAsync(request.Username, request.Password);

            if (authUser == null)
            {
                _logger.LogWarning("Failed login attempt for user {Username}", request.Username);
                return new AuthResponse { isSuccessful = false, Message = "Invalid username or password" };
            }

            // Check if account is locked
            if (IsAccountLocked(authUser))
            {
                return new AuthResponse
                {
                    isSuccessful = false,
                    Message = $"Account is locked due to too many failed attempts. Try again later."
                };
            }

            // Generate access token (your DTOs use a single Token field)
            var accessToken = GenerateJwtToken(authUser);
            var refreshToken = GenerateRefreshToken();

            // Update user with refresh token
            authUser.RefreshToken = refreshToken;
            authUser.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7); // 7 days
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("User {Username} logged in successfully", authUser.Username);

            return new AuthResponse
            {
                isSuccessful = true,
                Message = "Login successful",
                Username = authUser.Username,
                Token = accessToken // Using your DTO structure
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Username}", request.Username);
            return new AuthResponse { isSuccessful = false, Message = "Login failed due to an internal error" };
        }
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var user = await _dbSet
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

            if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
            {
                return new AuthResponse { isSuccessful = false, Message = "Invalid or expired refresh token" };
            }

            // Generate new access token
            var newAccessToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _dbContext.SaveChangesAsync();

            return new AuthResponse
            {
                isSuccessful = true,
                Token = newAccessToken,
                Username = user.Username,
                Message = "Token refreshed successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return new AuthResponse { isSuccessful = false, Message = "Token refresh failed" };
        }
    }

    public async Task InvalidateRefreshTokenAsync(Guid userId)
    {
        var user = await _dbSet.FirstOrDefaultAsync(u => u.Id == userId);
        if (user != null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            await _dbContext.SaveChangesAsync();
        }
    }

    private bool IsAccountLocked(AuthUser user)
    {
        return user.FailedLoginAttempts > 5;
    }

    private string GenerateJwtToken(AuthUser user)
    {
        return JwtTokenGenerator.GenerateJwtToken(user, _configuration);
    }

    private string GenerateRefreshToken()
    {
        return JwtTokenGenerator.GenerateJwtToken(64);
    }
}
