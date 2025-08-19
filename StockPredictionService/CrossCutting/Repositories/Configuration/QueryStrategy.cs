namespace StockPredictionService.CrossCutting.Repositories.Configuration;

public enum QueryStrategy
{
    Standard,
    Chunked,
    Parallel,
    MemoryMapped,
    Streaming
}

