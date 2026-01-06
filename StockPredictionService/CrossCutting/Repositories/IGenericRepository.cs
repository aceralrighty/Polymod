using System.Linq.Expressions;
using StockPredictionService.CrossCutting.Repositories.Configuration;

namespace StockPredictionService.CrossCutting.Repositories;

public interface IGenericRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);
    Task<List<T>> GetAllChunkedAsync(int chunkSize, CancellationToken ct = default);
    Task<List<T>> GetAllOptimizedAsync(CancellationToken ct = default);
    IAsyncEnumerable<T> GetAllStreamingAsync(int bufferSize, CancellationToken ct = default);
    Task<List<T>> GetAllParallelAsync(int partitionCount, CancellationToken ct = default);
    IAsyncEnumerable<T> GetAllMemoryMappedAsync(CancellationToken ct = default);
    Task<List<T>> GetAllConfigurableAsync(QueryOptions options, CancellationToken ct = default);
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);


    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task BulkInsertAsync(IEnumerable<T> entities);
}
