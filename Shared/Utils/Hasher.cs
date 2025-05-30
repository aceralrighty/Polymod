using Isopoh.Cryptography.Argon2;

namespace TBD.Shared.Utils;

public static class Hasher
{
    public static string HashPassword(string password)
    {
        
        return Argon2.Hash(password, timeCost: 4, memoryCost: 32768, parallelism: 2,
            type: Argon2Type.DataDependentAddressing, hashLength: 32);
    }

    public static bool Verify(string encodedHash, string password)
    {
        return Argon2.Verify(encodedHash, password);
    }
}