using Isopoh.Cryptography.Argon2;

namespace TBD.Shared.Utils;

/**
 *<summary>
 * this Hasher adapts the hashing algorithm to the available memory and CPU cores (based on system environment), as to not allow for GC pressure and avoid LOH allocation.
 * </summary>
 */
public class Hasher : IHasher
{
    private static readonly HashingConfig Config = DetermineOptimalConfig();

    private class HashingConfig
    {
        public int MemoryCost { get; init; }
        public int TimeCost { get; init; }
        public int Parallelism { get; init; }
    }

    private static HashingConfig DetermineOptimalConfig()
    {
        // Determine available memory and CPU cores
        var availableMemoryMb = GC.GetTotalMemory(false) / (1024 * 1024);
        var processorCount = Environment.ProcessorCount;

        // Conservative settings that avoid LOH
        return new HashingConfig
        {
            MemoryCost = Math.Min(16384, (int)(availableMemoryMb * 0.1)), // Max 16MB or 10% of available
            TimeCost = availableMemoryMb > 1000 ? 4 : 8, // More time if less memory
            Parallelism = Math.Min(processorCount, 4) // Limit parallelism
        };
    }

    // Now explicit implementation of interface methods
    public string HashPassword(string password)
    {
        return Argon2.Hash(
            password,
            timeCost: Config.TimeCost,
            memoryCost: Config.MemoryCost,
            parallelism: Config.Parallelism,
            type: Argon2Type.DataIndependentAddressing,
            hashLength: 32
        );
    }

    public bool Verify(string encodedHash, string password)
    {
        return Argon2.Verify(encodedHash, password);
    }
}
