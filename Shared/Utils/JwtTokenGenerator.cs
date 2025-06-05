using System.Security.Cryptography;

namespace TBD.Shared.Utils;

public class JwtTokenGenerator
{
    public static string GenerateJwtToken(int length = 32)
    {
        var randomBytes = new byte[length];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        return Convert.ToBase64String(randomBytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
