using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using App.Metrics;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace TBD.Shared.Repositories;

public class GenericRepository<T>(DbContext context) : IGenericRepository<T>
    where T : class
{
    protected readonly DbContext Context = context;
    protected readonly DbSet<T> DbSet = context.Set<T>();
    private readonly IDbConnection _dbConnection = context.Database.GetDbConnection();


    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await DbSet.ToListAsync();
    }

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

    /**
     * A helper method to get the table name from the entity type. So I don't do SQL injection
     */
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

        // Get connection
        if (_dbConnection.State != ConnectionState.Open)
            await ((DbConnection)_dbConnection).OpenAsync();

        using var sqlBulk = new SqlBulkCopy((SqlConnection)_dbConnection, SqlBulkCopyOptions.Default, null);
        sqlBulk.DestinationTableName = tableName;
        sqlBulk.BatchSize = 1000;

        var table = ToDataTable(enumerable);

        // Map columns automatically
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
