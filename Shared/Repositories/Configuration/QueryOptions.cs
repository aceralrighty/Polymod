namespace TBD.Shared.Repositories.Configuration;

public class QueryOptions
{
    public QueryStrategy Strategy { get; set; } = QueryStrategy.Standard;
    public int ChunkSize { get; set; } = 10000;
    public int ParallelPartitions { get; set; } = 4;
    public int StreamingBufferSize { get; set; } = 5000;
    public int CommandTimeout { get; set; } = 300; // seconds
}
