namespace PolyMod.Shared.Repositories.Configuration;

public enum QueryStrategy
{
    Standard,
    Chunked,
    Parallel,
    MemoryMapped,
    Streaming
}

