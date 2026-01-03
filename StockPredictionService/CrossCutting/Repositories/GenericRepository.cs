using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using StockPredictionService.CrossCutting.Repositories.Configuration;

namespace StockPredictionService.CrossCutting.Repositories;

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
        if (_isRelational == false) return false;
        if (_dbConnection is not null) return true;

        _isRelational = Context.Database.IsRelational();
        if (_isRelational.HasValue)
        {
            _dbConnection = Context.Database.GetDbConnection();
        }

        return _isRelational.Value;
    }

    // Original method (kept for compatibility)
    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
    {
        return await DbSet.ToListAsync(ct).ConfigureAwait(false);
    }

    // High-performance method using raw SQL with Dapper
    public virtual async Task<List<T>> GetAllOptimizedAsync(CancellationToken ct = default)
    {
        if (!EnsureRelational() || (_dbConnection ??= Context.Database.GetDbConnection()) is null)
            return await DbSet.ToListAsync(ct).ConfigureAwait(false);
        try
        {
            if (_dbConnection.State != ConnectionState.Open)
                await _dbConnection.OpenAsync(ct).ConfigureAwait(false);
            var tableName = GetTableName();
            var sql = $"SELECT * FROM {tableName} WITH (NOLOCK)";

            var result = await _dbConnection.QueryAsync<T>(new CommandDefinition(sql, cancellationToken: ct))
                .ConfigureAwait(false);
            return [.. result];
        }
        catch (Exception)
        {
            return await DbSet.AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
        }
    }

    // Chunked/Batched approach for very large datasets
    public virtual async Task<List<T>> GetAllChunkedAsync(int chunkSize = 10000, CancellationToken ct = default)
    {
        if (!EnsureRelational() || _dbConnection is null)
            return await DbSet.ToListAsync(ct).ConfigureAwait(false);

        if (_dbConnection.State != ConnectionState.Open)
            await _dbConnection.OpenAsync(ct).ConfigureAwait(false);

        var tableName = GetTableName();
        var allResults = new List<T>();
        var offset = 0;
        var hasMoreData = true;

        // Get the total count first
        var countSql = $"SELECT COUNT(*) FROM {tableName}";
        var totalCount = await _dbConnection.QuerySingleAsync<int>(countSql).ConfigureAwait(false);

        Console.WriteLine($"📊 Total records to fetch: {totalCount:N0}");

        while (hasMoreData)
        {
            var sql = $"""

                                       SELECT * FROM {tableName}
                                       ORDER BY Id
                                       OFFSET @Offset ROWS
                                       FETCH NEXT @ChunkSize ROWS ONLY
                       """;

            var chunk = await _dbConnection.QueryAsync<T>(sql, new { Offset = offset, ChunkSize = chunkSize })
                .ConfigureAwait(false);
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
    public virtual async IAsyncEnumerable<T> GetAllStreamingAsync(int bufferSize = 5000,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!EnsureRelational() || _dbConnection is null)
        {
            var list = await DbSet.ToListAsync(ct).ConfigureAwait(false);
            foreach (var item in list)
                yield return item;
            yield break;
        }

        if (_dbConnection.State != ConnectionState.Open)
            await _dbConnection.OpenAsync(ct).ConfigureAwait(false);

        var tableName = GetTableName();
        var sql = $"SELECT * FROM {tableName} ORDER BY Id";

        await using var command = new SqlCommand(sql, (SqlConnection)_dbConnection);
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);

        var properties = GetMappedProperties();
        var buffer = new List<T>(bufferSize);

        while (await reader.ReadAsync(ct).ConfigureAwait(false))
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
    public virtual async Task<List<T>> GetAllParallelAsync(int partitionCount = 4, CancellationToken ct = default)
    {
        if (!EnsureRelational())
            return await DbSet.AsNoTracking().ToListAsync(ct);

        try
        {
            var tableName = GetTableName();
            var totalCount =
                await _dbConnection!.ExecuteScalarAsync<int>(new CommandDefinition($"SELECT COUNT(*) FROM {tableName}",
                    cancellationToken: ct));
            var recordsPerPartition = (int)Math.Ceiling((double)totalCount / partitionCount);

            var tasks = Enumerable.Range(0, partitionCount).Select(async i =>
            {
                // Check if cancelled before starting a new connection
                ct.ThrowIfCancellationRequested();

                await using var conn = new SqlConnection(((SqlConnection)_dbConnection).ConnectionString);

                var offset = i * recordsPerPartition;
                var sql = $"""
                           SELECT * FROM {tableName}
                           ORDER BY Id
                           OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY
                           """;

                var result = await conn.QueryAsync<T>(new CommandDefinition(sql,
                    new { Offset = offset, Limit = recordsPerPartition }, cancellationToken: ct));
                return result.ToList();
            });

            var results = await Task.WhenAll(tasks);
            return [.. results.SelectMany(r => r)];
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return await DbSet.AsNoTracking().ToListAsync(ct);
        }
    }

    // Memory-mapped approach for extremely large datasets
    public virtual async IAsyncEnumerable<T> GetAllMemoryMappedAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!EnsureRelational())
        {
            // Fallback: Use EF's built-in AsAsyncEnumerable
            await foreach (var item in DbSet.AsNoTracking().AsAsyncEnumerable().WithCancellation(ct))
                yield return item;
            yield break;
        }

        _dbConnection ??= Context.Database.GetDbConnection();
        if (_dbConnection.State != ConnectionState.Open)
            await _dbConnection.OpenAsync(ct).ConfigureAwait(false);

        var tableName = GetTableName();
        var sql = $"SELECT * FROM {tableName} ORDER BY Id";

        // CommandBehavior.SequentialAccess is the real secret sauce for memory efficiency
        await using var command = _dbConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 300;

        await using var reader =
            await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, ct).ConfigureAwait(false);
        var properties = GetMappedProperties();

        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            // Map and yield immediately so we don't hold a giant list in memory
            yield return MapReaderToEntity(reader, properties);
        }
    }

    public virtual async Task<List<T>> GetAllConfigurableAsync(QueryOptions? options = null,
        CancellationToken ct = default)
    {
        if (EnsureRelational() && _dbConnection is not null)
        {
            return options is null
                ? await GetAllOptimizedAsync(ct)
                : options.Strategy switch
                {
                    QueryStrategy.Standard => await GetAllOptimizedAsync(ct),
                    QueryStrategy.Chunked => await GetAllChunkedAsync(options.ChunkSize, ct),
                    QueryStrategy.Parallel => await GetAllParallelAsync(options.ParallelPartitions, ct),
                    QueryStrategy.MemoryMapped => await GetAllMemoryMappedAsync(ct).ToListAsync(ct),
                    _ => await GetAllOptimizedAsync(ct)
                };
        }

        // Provider-agnostic fallbacks by strategy
        if (options is null) return await DbSet.ToListAsync(ct).ConfigureAwait(false);
        return options.Strategy switch
        {
            QueryStrategy.Standard => await DbSet.ToListAsync(ct).ConfigureAwait(false),
            QueryStrategy.Chunked => await DbSet.AsNoTracking().ToListAsync(ct).ConfigureAwait(false),
            QueryStrategy.Parallel => await DbSet.AsNoTracking().ToListAsync(ct).ConfigureAwait(false),
            QueryStrategy.MemoryMapped => await DbSet.AsNoTracking().ToListAsync(ct).ConfigureAwait(false),
            _ => await DbSet.ToListAsync(ct).ConfigureAwait(false)
        };
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (!EnsureRelational() || _dbConnection is null)
        {
            return await DbSet.FindAsync([id], ct).ConfigureAwait(false);
        }

        if (_dbConnection.State != ConnectionState.Open)
            await _dbConnection.OpenAsync(ct).ConfigureAwait(false);

        var tableName = GetTableName();
        var sql = $"SELECT * FROM {tableName} WHERE Id = @Id";
        return await _dbConnection
            .QueryFirstOrDefaultAsync<T>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct))
            .ConfigureAwait(false);
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate,
        CancellationToken ct = default)
    {
        return await DbSet.Where(predicate).ToListAsync(ct).ConfigureAwait(false);
    }


    // Helper method to get mapped properties
    private static PropertyInfo[] GetMappedProperties()
    {
        return typeof(T).GetProperties()
            .Where(p => p.CanWrite &&
                        p.GetCustomAttributes(typeof(NotMappedAttribute), true).Length == 0)
            .ToArray();
    }

    // Helper method to map DbDataReader to entity
    private static T MapReaderToEntity(DbDataReader reader, PropertyInfo[] properties)
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
                    QueryStrategy.MemoryMapped => await GetAllMemoryMappedAsync().ToListAsync(),
                    _ => await GetAllOptimizedAsync()
                };
        }

        // Provider-agnostic fallbacks by strategy
        if (options is null) return await DbSet.ToListAsync().ConfigureAwait(false);
        return options.Strategy switch
        {
            QueryStrategy.Standard => await DbSet.ToListAsync().ConfigureAwait(false),
            QueryStrategy.Chunked => await DbSet.AsNoTracking().ToListAsync()
                .ConfigureAwait(false), // simple fallback
            QueryStrategy.Parallel => await DbSet.AsNoTracking().ToListAsync()
                .ConfigureAwait(false), // simple fallback
            QueryStrategy.MemoryMapped => await DbSet.AsNoTracking().ToListAsync()
                .ConfigureAwait(false), // simple fallback
            _ => await DbSet.ToListAsync().ConfigureAwait(false)
        };
    }

    // Existing methods remain unchanged...
    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        if (EnsureRelational() && _dbConnection is not null)
        {
            if (_dbConnection.State != ConnectionState.Open)
                await _dbConnection.OpenAsync().ConfigureAwait(false);

            var tableName = GetTableName();
            var sql = $"SELECT * FROM {tableName} WHERE Id = @Id";
            return await _dbConnection.QueryFirstOrDefaultAsync<T>(sql, new { Id = id }).ConfigureAwait(false) ??
                   throw new NullReferenceException();
        }

        // Provider-agnostic fallback
        var entity = await DbSet.FindAsync(id).ConfigureAwait(false);
        return entity ?? throw new NullReferenceException();
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await DbSet.Where(predicate).ToListAsync().ConfigureAwait(false);
    }

    public virtual async Task AddAsync(T entity)
    {
        await DbSet.AddAsync(entity).ConfigureAwait(false);
        await Context.SaveChangesAsync().ConfigureAwait(false);
    }

    public virtual async Task UpdateAsync(T entity)
    {
        DbSet.Update(entity);
        await Context.SaveChangesAsync().ConfigureAwait(false);
    }

    public virtual async Task DeleteAsync(T entity)
    {
        DbSet.Remove(entity);
        await Context.SaveChangesAsync().ConfigureAwait(false);
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
            await DbSet.AddRangeAsync(enumerable).ConfigureAwait(false);
            await Context.SaveChangesAsync().ConfigureAwait(false);
            return;
        }

        if (_dbConnection.State != ConnectionState.Open)
            await _dbConnection.OpenAsync().ConfigureAwait(false);

        using var sqlBulk = new SqlBulkCopy((SqlConnection)_dbConnection, SqlBulkCopyOptions.Default, null);
        sqlBulk.DestinationTableName = tableName;
        sqlBulk.BatchSize = 1000;

        var table = ToDataTable(enumerable);

        foreach (DataColumn column in table.Columns)
        {
            sqlBulk.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        }

        await sqlBulk.WriteToServerAsync(table).ConfigureAwait(false);
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
