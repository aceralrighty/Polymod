using System.Linq.Expressions;
using StockPredictionService.CrossCutting.Repositories.Configuration;

namespace StockPredictionService.CrossCutting.Repositories;

public interface IGenericRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<List<T>> GetAllChunkedAsync(int chunkSize);
    Task<List<T>> GetAllOptimizedAsync();
    IAsyncEnumerable<T> GetAllStreamingAsync(int bufferSize);
    Task<List<T>> GetAllParallelAsync(int partitionCount);
    Task<List<T>> GetAllMemoryMappedAsync();
    Task<List<T>> GetAllConfigurableAsync(QueryOptions options);
    Task BulkInsertAsync(IEnumerable<T> entities);

    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);

}
