using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TBD.Shared.Repositories.Configuration;

namespace TBD.Shared.Repositories;

public class GenericRepository<T>(DbContext context) : IGenericRepository<T>
    where T : class
{
    protected readonly DbContext Context = context;
    protected readonly DbSet<T> DbSet = context.Set<T>();
    private readonly IDbConnection _dbConnection = context.Database.GetDbConnection();

    // Original method (kept for compatibility)
    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await DbSet.ToListAsync();
    }

    // High-performance method using raw SQL with Dapper
    public virtual async Task<List<T>> GetAllOptimizedAsync()
    {
        if (_dbConnection.State != ConnectionState.Open)
            await ((DbConnection)_dbConnection).OpenAsync();

        var tableName = GetTableName();
        var sql = $"SELECT * FROM {tableName} WITH (NOLOCK)";

        var result = await _dbConnection.QueryAsync<T>(sql);
        return result.ToList();
    }

    // Chunked/Batched approach for very large datasets
    public virtual async Task<List<T>> GetAllChunkedAsync(int chunkSize = 10000)
    {
        if (_dbConnection.State != ConnectionState.Open)
            await ((DbConnection)_dbConnection).OpenAsync();

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
        if (_dbConnection.State != ConnectionState.Open)
            await ((DbConnection)_dbConnection).OpenAsync();

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
        if (_dbConnection.State != ConnectionState.Open)
            await ((DbConnection)_dbConnection).OpenAsync();

        var tableName = GetTableName();

        // Get total count and ID range
        var countSql = $"SELECT COUNT(*), MIN(Id), MAX(Id) FROM {tableName}";
        var (totalCount, minId, maxId) = await _dbConnection.QuerySingleAsync<(int, Guid, Guid)>(countSql);

        Console.WriteLine($"📊 Processing {totalCount:N0} records across {partitionCount} partitions");

        // Create tasks for parallel processing
        var tasks = new List<Task<List<T>>>();
        var recordsPerPartition = totalCount / partitionCount;

        for (int i = 0; i < partitionCount; i++)
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
        if (_dbConnection.State != ConnectionState.Open)
            await ((DbConnection)_dbConnection).OpenAsync();

        var tableName = GetTableName();

        // Use streaming with minimal memory allocation
        var sql = $"SELECT * FROM {tableName} ORDER BY Id";

        var results = new List<T>();
        var processed = 0;

        await using var command = new SqlCommand(sql, (SqlConnection)_dbConnection);
        command.CommandTimeout = 300; // 5 minutes timeout

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
        var properties = GetMappedProperties();

        while (await reader.ReadAsync())
        {
            var entity = MapReaderToEntity(reader, properties);
            results.Add(entity);
            processed++;

            if (processed % 10000 != 0)
            {
                continue;
            }

            Console.WriteLine($"📈 Processed {processed:N0} records");

            // Force garbage collection periodically to manage memory
            if (processed % 50000 != 0)
            {
                continue;
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Console.WriteLine($"✅ Total processed: {processed:N0} records");
        return results;
    }


    // Helper method to get mapped properties
    private PropertyInfo[] GetMappedProperties()
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
    public virtual async Task<List<T>> GetAllConfigurableAsync(QueryOptions options = null)
    {
        return options.Strategy switch
        {
            QueryStrategy.Standard => await GetAllOptimizedAsync(),
            QueryStrategy.Chunked => await GetAllChunkedAsync(options.ChunkSize),
            QueryStrategy.Parallel => await GetAllParallelAsync(options.ParallelPartitions),
            QueryStrategy.MemoryMapped => await GetAllMemoryMappedAsync(),
            _ => await GetAllOptimizedAsync()
        };
    }

    // Existing methods remain unchanged...
    public virtual async Task<T> GetByIdAsync(Guid id)
    {
        if (_dbConnection.State != ConnectionState.Open)
            await ((DbConnection)_dbConnection).OpenAsync();

        var tableName = GetTableName();
        var sql = $"SELECT * FROM {tableName} WHERE Id = @Id";
        return await _dbConnection.QueryFirstOrDefaultAsync<T>(sql, new { Id = id }) ??
               throw new NullReferenceException();
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

        if (_dbConnection.State != ConnectionState.Open)
            await ((DbConnection)_dbConnection).OpenAsync();

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

    private DataTable ToDataTable(IEnumerable<T> data)
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
