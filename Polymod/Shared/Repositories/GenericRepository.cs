using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PolyMod.Shared.Repositories.Configuration;

namespace PolyMod.Shared.Repositories;

public class GenericRepository<T>(DbContext context) : IGenericRepository<T>
    where T : class
{
    protected readonly DbContext Context = context;
    protected readonly DbSet<T> DbSet = context.Set<T>();

    // Avoid accessing relational APIs unless the provider is relational
    private bool? _isRelational;
    private DbConnection? _dbConnection;

    private bool EnsureRelational()
    {
        if (_isRelational.HasValue)
            return _isRelational.Value;

        var rel = Context.Database.IsRelational();
        _isRelational = rel;
        if (rel)
        {
            _dbConnection = Context.Database.GetDbConnection();
        }
        return rel;
    }

    // Original method (kept for compatibility)
    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await DbSet.ToListAsync();
    }

    // High-performance method using raw SQL with Dapper
    public virtual async Task<List<T>> GetAllOptimizedAsync()
    {
        if (!EnsureRelational() || _dbConnection is null)
            return await DbSet.ToListAsync();

        if (_dbConnection.State != ConnectionState.Open)
            await _dbConnection.OpenAsync();

        var tableName = GetTableName();
        var sql = $"SELECT * FROM {tableName} WITH (NOLOCK)";

        var result = await _dbConnection.QueryAsync<T>(sql);
        return result.ToList();
    }

    // Chunked/Batched approach for very large datasets
    public virtual async Task<List<T>> GetAllChunkedAsync(int chunkSize = 10000)
    {
        if (!EnsureRelational() || _dbConnection is null)
            return await DbSet.ToListAsync();

        if (_dbConnection.State != ConnectionState.Open)
            await _dbConnection.OpenAsync();

        var tableName = GetTableName();
        var allResults = new List<T>();
        var offset = 0;
        var hasMoreData = true;

        // Get the total count first
        var countSql = $"SELECT COUNT(*) FROM {tableName}";
        var totalCount = await _dbConnection.QuerySingleAsync<int>(countSql);

        Console.WriteLine($"📊 Total records to fetch: {totalCount:N0}");

        while (hasMoreData)
        {
            var sql = $@"
                SELECT * FROM {tableName}
                ORDER BY Id
                OFFSET @Offset ROWS
                FETCH NEXT @ChunkSize ROWS ONLY";

            var chunk = await _dbConnection.QueryAsync<T>(sql, new { Offset = offset, ChunkSize = chunkSize });
            var chunkList = chunk.ToList();

            if (chunkList.Count == 0)
            {
                hasMoreData = false;
            }
            else
            {
                allResults.AddRange(chunkList);
                offset += chunkSize;

                Console.WriteLine(
                    $"📈 Fetched {allResults.Count:N0}/{totalCount:N0} records ({(double)allResults.Count / totalCount * 100:F1}%)");
            }
        }

        return allResults;
    }

    // Streaming approach for memory-efficient processing
    public virtual async IAsyncEnumerable<T> GetAllStreamingAsync(int bufferSize = 5000)
    {
        if (!EnsureRelational() || _dbConnection is null)
        {
            var list = await DbSet.ToListAsync();
            foreach (var item in list)
                yield return item;
            yield break;
        }

        if (_dbConnection.State != ConnectionState.Open)
            await _dbConnection.OpenAsync();

        var tableName = GetTableName();
        var sql = $"SELECT * FROM {tableName} ORDER BY Id";

        await using var command = new SqlCommand(sql, (SqlConnection)_dbConnection);
        await using var reader = await command.ExecuteReaderAsync();

        var properties = GetMappedProperties();
        var buffer = new List<T>(bufferSize);

        while (await reader.ReadAsync())
        {
            var entity = MapReaderToEntity(reader, properties);
            buffer.Add(entity);

            if (buffer.Count < bufferSize)
            {
                continue;
            }

            foreach (var item in buffer)
                yield return item;

            buffer.Clear();
        }

        // Return remaining items
        foreach (var item in buffer)
            yield return item;
    }

    // Parallel processing approach for very large datasets
    public virtual async Task<List<T>> GetAllParallelAsync(int partitionCount = 4)
    {
        if (!EnsureRelational() || _dbConnection is null)
            return await DbSet.ToListAsync();

        if (_dbConnection.State != ConnectionState.Open)
            await _dbConnection.OpenAsync();

        var tableName = GetTableName();

        // Get total count and ID range
        var countSql = $"SELECT COUNT(*), MIN(Id), MAX(Id) FROM {tableName}";
        var (totalCount, _, _) = await _dbConnection.QuerySingleAsync<(int, Guid, Guid)>(countSql);

        Console.WriteLine($"📊 Processing {totalCount:N0} records across {partitionCount} partitions");

        // Create tasks for parallel processing
        var tasks = new List<Task<List<T>>>();
        var recordsPerPartition = totalCount / partitionCount;

        for (var i = 0; i < partitionCount; i++)
        {
            var partitionIndex = i;
            var task = Task.Run(async () =>
            {
                await using var connection = new SqlConnection(((SqlConnection)_dbConnection).ConnectionString);
                await connection.OpenAsync();

                var offset = partitionIndex * recordsPerPartition;
                var fetchSize = partitionIndex == partitionCount - 1 ? totalCount - offset : recordsPerPartition;

                var sql = $@"
                    SELECT * FROM {tableName}
                    ORDER BY Id
                    OFFSET @Offset ROWS
                    FETCH NEXT @FetchSize ROWS ONLY";

                var result = await connection.QueryAsync<T>(sql, new { Offset = offset, FetchSize = fetchSize });
                var partitionResults = result.ToList();

                Console.WriteLine($"🔧 Partition {partitionIndex + 1} completed: {partitionResults.Count:N0} records");
                return partitionResults;
            });

            tasks.Add(task);
        }

        var results = await Task.WhenAll(tasks);
        var allResults = results.SelectMany(r => r).ToList();

        Console.WriteLine($"✅ All partitions completed: {allResults.Count:N0} total records");
        return allResults;
    }

    // Memory-mapped approach for extremely large datasets
    public virtual async Task<List<T>> GetAllMemoryMappedAsync()
    {
        if (!EnsureRelational() || _dbConnection is null)
            return await DbSet.ToListAsync();

        const int gcCleaner = 50_000;
        const int progressCheck = 10_000;
        if (_dbConnection.State != ConnectionState.Open)
            await _dbConnection.OpenAsync();

        var tableName = GetTableName();

        // Use streaming with minimal memory allocation
        var sql = $"SELECT * FROM {tableName} ORDER BY Id";

        var results = new List<T>();
        var processed = 0;

        await using var command = new SqlCommand(sql, (SqlConnection)_dbConnection);
        command.CommandTimeout = 300; // 5-minute timeout

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
        var properties = GetMappedProperties();

        while (await reader.ReadAsync())
        {
            var entity = MapReaderToEntity(reader, properties);
            results.Add(entity);
            processed++;

            if (!IsMultipleOf(processed, progressCheck)) continue;

            Console.WriteLine($"📈 Processed {processed:N0} records");

            // Force garbage collection periodically to manage memory
            if (!IsMultipleOf(processed, gcCleaner)) continue;

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Console.WriteLine($"✅ Total processed: {processed:N0} records");
        return results;
    }

    // Helper method to check if a number is a multiple of another number
    private static bool IsMultipleOf(int processed, int factor) => factor != 0 && processed % factor == 0;

    // Helper method to get mapped properties
    private static PropertyInfo[] GetMappedProperties()
    {
        return typeof(T).GetProperties()
            .Where(p => p.CanWrite &&
                        p.GetCustomAttributes(typeof(NotMappedAttribute), true).Length == 0)
            .ToArray();
    }

    // Helper method to map SqlDataReader to entity
    private static T MapReaderToEntity(SqlDataReader reader, PropertyInfo[] properties)
    {
        var entity = Activator.CreateInstance<T>();

        for (var i = 0; i < reader.FieldCount; i++)
        {
            var columnName = reader.GetName(i);
            var property = properties.FirstOrDefault(p =>
                p.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase));

            if (property == null || reader.IsDBNull(i))
            {
                continue;
            }

            var value = reader.GetValue(i);

            // Handle type conversion if necessary
            if (value.GetType() != property.PropertyType)
            {
                var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                value = Convert.ChangeType(value, targetType);
            }

            property.SetValue(entity, value);
        }

        return entity;
    }

    // Enhanced method with configurable options
    public virtual async Task<List<T>> GetAllConfigurableAsync(QueryOptions? options = null)
    {
        if (EnsureRelational() && _dbConnection is not null)
        {
            return options is null
                ? await GetAllOptimizedAsync()
                : options.Strategy switch
                {
                    QueryStrategy.Standard => await GetAllOptimizedAsync(),
                    QueryStrategy.Chunked => await GetAllChunkedAsync(options.ChunkSize),
                    QueryStrategy.Parallel => await GetAllParallelAsync(options.ParallelPartitions),
                    QueryStrategy.MemoryMapped => await GetAllMemoryMappedAsync(),
                    _ => await GetAllOptimizedAsync()
                };
        }

        // Provider-agnostic fallbacks by strategy
        if (options is null) return await DbSet.ToListAsync();
        return options.Strategy switch
        {
            QueryStrategy.Standard => await DbSet.ToListAsync(),
            QueryStrategy.Chunked => await DbSet.AsNoTracking().ToListAsync(), // simple fallback
            QueryStrategy.Parallel => await DbSet.AsNoTracking().ToListAsync(), // simple fallback
            QueryStrategy.MemoryMapped => await DbSet.AsNoTracking().ToListAsync(), // simple fallback
            _ => await DbSet.ToListAsync()
        };

    }

    // Existing methods remain unchanged...
    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        if (EnsureRelational() && _dbConnection is not null)
        {
            if (_dbConnection.State != ConnectionState.Open)
                await _dbConnection.OpenAsync();

            var tableName = GetTableName();
            var sql = $"SELECT * FROM {tableName} WHERE Id = @Id";
            return await _dbConnection.QueryFirstOrDefaultAsync<T>(sql, new { Id = id }) ??
                   throw new NullReferenceException();
        }

        // Provider-agnostic fallback
        var entity = await DbSet.FindAsync(id);
        return entity ?? throw new NullReferenceException();
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await DbSet.Where(predicate).ToListAsync();
    }

    public virtual async Task AddAsync(T entity)
    {
        await DbSet.AddAsync(entity);
        await Context.SaveChangesAsync();
    }

    public virtual async Task UpdateAsync(T entity)
    {
        DbSet.Update(entity);
        await Context.SaveChangesAsync();
    }

    public virtual async Task DeleteAsync(T entity)
    {
        DbSet.Remove(entity);
        await Context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<T> entities)
    {
        await DbSet.AddRangeAsync(entities);
        await Context.SaveChangesAsync();
    }

    private string GetTableName()
    {
        var entityType = Context.Model.FindEntityType(typeof(T));
        if (entityType == null)
            throw new InvalidOperationException($"Could not find entity type metadata for {typeof(T).Name}");

        var schema = entityType.GetSchema();
        var table = entityType.GetTableName();

        return !string.IsNullOrWhiteSpace(schema) ? $"{schema}.{table}" : table!;
    }

    public async Task BulkInsertAsync(IEnumerable<T> entities)
    {
        var enumerable = entities as T[] ?? entities.ToArray();
        if (enumerable.Length == 0)
            return;

        var tableName = GetTableName();

        if (!EnsureRelational() || _dbConnection is null)
        {
            await DbSet.AddRangeAsync(enumerable);
            await Context.SaveChangesAsync();
            return;
        }

        if (_dbConnection.State != ConnectionState.Open)
            await _dbConnection.OpenAsync();

        using var sqlBulk = new SqlBulkCopy((SqlConnection)_dbConnection, SqlBulkCopyOptions.Default, null);
        sqlBulk.DestinationTableName = tableName;
        sqlBulk.BatchSize = 1000;

        var table = ToDataTable(enumerable);

        foreach (DataColumn column in table.Columns)
        {
            sqlBulk.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        }

        await sqlBulk.WriteToServerAsync(table);
    }

    private static DataTable ToDataTable(IEnumerable<T> data)
    {
        var table = new DataTable();
        var props = typeof(T).GetProperties()
            .Where(p => p.CanRead && p.GetCustomAttributes(typeof(NotMappedAttribute), true).Length == 0)
            .ToArray();

        foreach (var prop in props)
        {
            var type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            table.Columns.Add(prop.Name, type);
        }

        foreach (var item in data)
        {
            var values = props.Select(p => p.GetValue(item) ?? DBNull.Value).ToArray();
            table.Rows.Add(values);
        }

        return table;
    }
}
