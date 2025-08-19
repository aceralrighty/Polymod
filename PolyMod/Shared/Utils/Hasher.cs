using Isopoh.Cryptography.Argon2;
using System.Runtime;

namespace TBD.Shared.Utils;

/**
 *<summary>
 * This Hasher adapts the hashing algorithm to the available memory and CPU cores,
 * optimized to minimize GC pressure and avoid LOH allocation.
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
        // Get actual system memory info
        var gcMemoryInfo = GC.GetGCMemoryInfo();
        var availableMemoryBytes = gcMemoryInfo.HighMemoryLoadThresholdBytes - gcMemoryInfo.MemoryLoadBytes;
        var availableMemoryMb = Math.Max(512, availableMemoryBytes / (1024 * 1024)); // Minimum 512MB assumption

        var processorCount = Environment.ProcessorCount;

        // Calculate memory cost in KB (Argon2 parameter is in KB)
        // LOH threshold is 85KB, so we want to stay well below that per thread
        // Argon2 memory usage ≈ memoryCost KB * parallelism
        const int maxMemoryPerThreadKb = 32; // Stay well below LOH threshold
        var totalMemoryBudgetKb = Math.Min(
            (int)(availableMemoryMb * 1024 * 0.05), // Use max 5% of available memory
            maxMemoryPerThreadKb * Math.Min(processorCount, 4) // Limit total based on parallelism
        );

        var parallelism = Math.Min(processorCount, 4);
        var memoryCostKb = Math.Max(8, totalMemoryBudgetKb / parallelism); // Minimum 8KB per thread

        return new HashingConfig
        {
            MemoryCost = memoryCostKb,
            TimeCost = memoryCostKb < 32 ? 6 : 3, // More iterations if less memory
            Parallelism = parallelism
        };
    }

    public string HashPassword(string password)
    {
        // Force garbage collection before intensive operation if needed
        if (GC.GetTotalMemory(false) > 100 * 1024 * 1024) // If > 100MB in managed heap
        {
            GC.Collect(0, GCCollectionMode.Optimized);
        }

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
        // Verification typically uses parameters from the hash itself,
        // so memory pressure should be less of an issue
        return Argon2.Verify(encodedHash, password);
    }

    // Optional: Method to check current configuration (useful for debugging)
    public (int MemoryCost, int TimeCost, int Parallelism) GetCurrentConfig()
    {
        return (Config.MemoryCost, Config.TimeCost, Config.Parallelism);
    }
}
