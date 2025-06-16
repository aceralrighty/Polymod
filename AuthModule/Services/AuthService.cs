using Microsoft.EntityFrameworkCore;
using TBD.API.DTOs.AuthDTO;
using TBD.AuthModule.Data;
using TBD.AuthModule.Exceptions;
using TBD.AuthModule.Models;
using TBD.AuthModule.Repositories;
using TBD.MetricsModule.Services;
using TBD.Shared.Utils;

namespace TBD.AuthModule.Services;

public class AuthService(
    IAuthRepository repository,
    AuthDbContext dbContext,
    IConfiguration configuration,
    ILogger<AuthService> logger,
    IHasher hasher,
    IMetricsServiceFactory metricsServiceFactory)
    : IAuthService
{
    private readonly IMetricsService _metricsService = metricsServiceFactory.CreateMetricsService("AuthModule");

    public async Task<AuthUser?> AuthenticateAsync(string username, string password)
    {
        _metricsService.IncrementCounter("auth.authentication_attempts");

        var authUser = await repository.GetUserByUsername(username);
        if (authUser == null)
        {
            _metricsService.IncrementCounter("auth.authentication_failed_user_not_found");
            logger.LogWarning("Authentication failed - User not found: {Username}", username);
            return null;
        }

        if (!hasher.Verify(authUser.HashedPassword, password))
        {
            authUser.FailedLoginAttempts++;
            _metricsService.IncrementCounter("auth.authentication_failed_wrong_password");
            _metricsService.IncrementCounter("auth.failed_login_attempts_incremented");

            logger.LogWarning(
                "Authentication failed - Wrong password for user: {Username}. Failed attempts: {FailedAttempts}",
                username, authUser.FailedLoginAttempts);

            await repository.UpdateAsync(authUser);
            return null;
        }

        // Successful authentication
        authUser.LastLogin = DateTime.UtcNow;
        authUser.FailedLoginAttempts = 0;
        _metricsService.IncrementCounter("auth.authentication_successful");

        logger.LogInformation("Authentication successful for user: {Username}", username);

        await repository.UpdateAsync(authUser);
        return authUser;
    }

    public async Task<AuthUser?> GetAuthUserByUsernameAsync(string username)
    {
        _metricsService.IncrementCounter("auth.user_lookup_by_username");
        var user = await repository.GetUserByUsername(username);

        if (user == null)
        {
            _metricsService.IncrementCounter("auth.user_lookup_not_found");
            logger.LogDebug("User lookup failed - User not found: {Username}", username);
        }
        else
        {
            _metricsService.IncrementCounter("auth.user_lookup_successful");
            logger.LogDebug("User lookup successful: {Username}", username);
        }

        return user;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            _metricsService.IncrementCounter("auth.registration_attempts");

            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                _metricsService.IncrementCounter("auth.registration_failed_missing_fields");
                logger.LogWarning("Registration failed - Missing required fields for username: {Username}",
                    request.Username);
                return new AuthResponse { IsSuccessful = false, Message = $"All fields are required to be filled in"};
            }

            var existingUser = await repository.GetUserByUsername(request.Username);
            if (existingUser != null)
            {
                _metricsService.IncrementCounter("auth.registration_failed_user_exists");
                logger.LogWarning("Registration failed - Username already exists: {Username}", request.Username);
                return new AuthResponse
                {
                    IsSuccessful = false, Message = $"Username {request.Username} already exists"
                };
            }

            if (request.Password.Length < 9)
            {
                _metricsService.IncrementCounter("auth.registration_failed_weak_password");
                logger.LogWarning("Registration failed - Password too short for username: {Username}",
                    request.Username);
                return new AuthResponse { IsSuccessful = false, Message = $"Password must be at least 9 characters" };
            }

            var createNewUser = new AuthUser
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                HashedPassword = hasher.HashPassword(request.Password),
                FailedLoginAttempts = 0,
            };

            await repository.AddAsync(createNewUser);
            _metricsService.IncrementCounter("auth.registration_successful");
            _metricsService.IncrementCounter("auth.users_created_total");

            logger.LogInformation("User registration successful: {Username} with email: {Email}",
                createNewUser.Username, createNewUser.Email);

            return new AuthResponse
            {
                IsSuccessful = true,
                Message = "Registration successful",
                Username = createNewUser.Username,
                Token = createNewUser.RefreshToken!
            };
        }
        catch (ErrorDuringUserRegistrationException ex)
        {
            _metricsService.IncrementCounter("auth.registration_failed_exception");
            logger.LogError(ex, "Registration failed with exception for username: {Username}", request.Username);
            return new AuthResponse { IsSuccessful = false, Message = ex.Message };
        }
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            _metricsService.IncrementCounter("auth.login_attempts");

            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                _metricsService.IncrementCounter("auth.login_failed_missing_credentials");
                logger.LogWarning("Login failed - Missing credentials for username: {Username}",
                    request.Username);
                return new AuthResponse { IsSuccessful = false, Message = "Username and password are required" };
            }

            var authUser = await AuthenticateAsync(request.Username, request.Password);

            if (authUser == null)
            {
                _metricsService.IncrementCounter("auth.login_failed_authentication");
                logger.LogWarning("Login failed - Authentication failed for user: {Username}", request.Username);
                return new AuthResponse { IsSuccessful = false, Message = "Invalid username or password" };
            }

            // Check if the account is locked
            if (IsAccountLocked(authUser))
            {
                _metricsService.IncrementCounter("auth.login_failed_account_locked");
                logger.LogWarning(
                    "Login failed - Account locked for user: {Username}. Failed attempts: {FailedAttempts}",
                    request.Username, authUser.FailedLoginAttempts);
                return new AuthResponse
                {
                    IsSuccessful = false,
                    Message = "Account is locked due to too many failed attempts. Try again later."
                };
            }

            // Generate tokens
            var accessToken = GenerateJwtToken(authUser);
            var refreshToken = GenerateRefreshToken();

            // Update user with refresh token
            authUser.RefreshToken = refreshToken;
            authUser.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            _metricsService.IncrementCounter("auth.login_successful");
            _metricsService.IncrementCounter("auth.refresh_tokens_generated");

            await repository.UpdateAsync(authUser);

            logger.LogInformation("Login successful for user: {Username}. New refresh token expiry: {TokenExpiry}",
                authUser.Username, authUser.RefreshTokenExpiry);

            return new AuthResponse
            {
                IsSuccessful = true,
                Message = "Login successful",
                Username = authUser.Username,
                Token = accessToken,
                RefreshToken = refreshToken
            };
        }
        catch (Exception ex)
        {
            _metricsService.IncrementCounter("auth.login_failed_exception");
            logger.LogError(ex, "Login failed with exception for username: {Username}", request.Username);
            return new AuthResponse { IsSuccessful = false, Message = "Login failed due to an internal error" };
        }
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            _metricsService.IncrementCounter("auth.token_refresh_attempts");

            var user = await repository.GetUserByRefreshToken(refreshToken);

            if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
            {
                _metricsService.IncrementCounter("auth.token_refresh_failed_invalid_token");
                logger.LogWarning("Token refresh failed - Invalid or expired refresh token");
                return new AuthResponse { IsSuccessful = false, Message = "Invalid or expired refresh token" };
            }

            // Generate a new access token
            var newAccessToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await dbContext.SaveChangesAsync();

            _metricsService.IncrementCounter("auth.token_refresh_successful");
            _metricsService.IncrementCounter("auth.new_tokens_issued");

            logger.LogInformation("Token refresh successful for user: {Username}. New expiry: {TokenExpiry}",
                user.Username, user.RefreshTokenExpiry);

            return new AuthResponse
            {
                IsSuccessful = true,
                Token = newAccessToken,
                Username = user.Username,
                Message = "Token refreshed successfully"
            };
        }
        catch (Exception ex)
        {
            _metricsService.IncrementCounter("auth.token_refresh_failed_exception");
            logger.LogError(ex, "Token refresh failed with exception");
            return new AuthResponse { IsSuccessful = false, Message = "Token refresh failed" };
        }
    }

    public async Task InvalidateRefreshTokenAsync(Guid userId)
    {
        _metricsService.IncrementCounter("auth.token_invalidation_attempts");

        var user = await dbContext.AuthUsers.FirstOrDefaultAsync(u => u.Id == userId);
        if (user != null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            await dbContext.SaveChangesAsync();

            _metricsService.IncrementCounter("auth.token_invalidation_successful");
            logger.LogInformation("Refresh token invalidated for user: {Username} (ID: {UserId})",
                user.Username, userId);
        }
        else
        {
            _metricsService.IncrementCounter("auth.token_invalidation_failed_user_not_found");
            logger.LogWarning("Token invalidation failed - User not found with ID: {UserId}", userId);
        }
    }

    private bool IsAccountLocked(AuthUser user)
    {
        var isLocked = user.FailedLoginAttempts > 5;
        if (isLocked)
        {
            _metricsService.IncrementCounter("auth.account_locked_check_positive");
        }

        return isLocked;
    }

    private string GenerateJwtToken(AuthUser user)
    {
        _metricsService.IncrementCounter("auth.jwt_tokens_generated");
        return JwtTokenGenerator.GenerateJwtToken(user, configuration);
    }

    private string GenerateRefreshToken()
    {
        _metricsService.IncrementCounter("auth.refresh_tokens_created");
        return JwtTokenGenerator.GenerateJwtToken(64);
    }
}
