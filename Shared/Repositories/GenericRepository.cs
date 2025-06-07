using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace TBD.Shared.Repositories;

public class GenericRepository<T>(DbContext context) : IGenericRepository<T>
    where T : class
{
    protected readonly DbContext _context = context;
    protected readonly DbSet<T> _dbSet = context.Set<T>();
    protected readonly IDbConnection DbConnection = context.Database.GetDbConnection();

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<T> GetByIdAsync(Guid id)
    {
        if (DbConnection.State != ConnectionState.Open)
            await ((DbConnection)DbConnection).OpenAsync();

        var tableName = GetTableName();
        var sql = $"SELECT * FROM {tableName} WHERE Id = @Id";
        return await DbConnection.QueryFirstOrDefaultAsync<T>(sql, new { Id = id }) ?? throw new NullReferenceException();
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public virtual async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public virtual async Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }

    public virtual async Task DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }

    /**
     * A helper method to get the table name from the entity type. So I don't do SQL injection
     */
    protected virtual string GetTableName()
    {
        var entityType = _context.Model.FindEntityType(typeof(T));
        if (entityType == null)
            throw new InvalidOperationException($"Could not find entity type metadata for {typeof(T).Name}");

        var schema = entityType.GetSchema();
        var table = entityType.GetTableName();

        return !string.IsNullOrWhiteSpace(schema) ? $"{schema}.{table}" : table!;
    }
}
