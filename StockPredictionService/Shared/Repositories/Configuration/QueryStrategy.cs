namespace PolyMod.StockPredictionService.Shared.Repositories.Configuration;

public enum QueryStrategy
{
    Standard,
    Chunked,
    Parallel,
    MemoryMapped,
    Streaming
}

