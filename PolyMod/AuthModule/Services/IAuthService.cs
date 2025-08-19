using TBD.API.DTOs.AuthDTO;
using TBD.AuthModule.Models;

namespace TBD.AuthModule.Services;

public interface IAuthService
{
    Task<AuthUser?> AuthenticateAsync(string username, string password);
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);
    Task InvalidateRefreshTokenAsync(Guid userId);
}
