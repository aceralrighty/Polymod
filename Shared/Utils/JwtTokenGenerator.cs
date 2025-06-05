using System.Security.Cryptography;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TBD.AuthModule.Models;

namespace TBD.Shared.Utils;

public class JwtTokenGenerator
{
    // Your existing method for generating random tokens (for refresh tokens, etc.)
    public static string GenerateJwtToken(int length = 32)
    {
        var randomBytes = new byte[length];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        return Convert.ToBase64String(randomBytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    // New method for generating actual JWT tokens with user claims
    public static string GenerateJwtToken(AuthUser user, IConfiguration configuration)
    {
        var jwtKey = configuration["Jwt:Key"] ?? "your-super-secret-key-here-make-it-long-and-complex";
        var key = Encoding.ASCII.GetBytes(jwtKey);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("jti", Guid.NewGuid().ToString()), // JWT ID for uniqueness
            new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64) // Issued at
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(15), // Short-lived access token
            Issuer = configuration["Jwt:Issuer"] ?? "TBD-API",
            Audience = configuration["Jwt:Audience"] ?? "TBD-Client",
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    // Optional: Method to validate JWT tokens (useful for debugging)
    public static ClaimsPrincipal? ValidateToken(string token, IConfiguration configuration)
    {
        try
        {
            var jwtKey = configuration["Jwt:Key"] ?? "your-super-secret-key-here-make-it-long-and-complex";
            var key = Encoding.ASCII.GetBytes(jwtKey);

            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = configuration["Jwt:Issuer"] ?? "TBD-API",
                ValidateAudience = true,
                ValidAudience = configuration["Jwt:Audience"] ?? "TBD-Client",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    // Method to extract user ID from JWT token without full validation
    public static Guid? GetUserIdFromToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);
            var userIdClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
        }
        catch
        {
            // Token is malformed or invalid
        }

        return null;
    }
}
