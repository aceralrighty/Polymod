using System.Data;
using System.Linq.Expressions;
using Dapper;
using Microsoft.EntityFrameworkCore;
using TBD.ScheduleModule.Data;
using TBD.ScheduleModule.Models;

namespace TBD.ScheduleModule.Repositories;

public class GenericScheduleRepository<T> where T : class
{
    protected readonly ScheduleDbContext context;
    protected readonly DbSet<T> _dbSet;
    private readonly IDbConnection _connection;

    protected GenericScheduleRepository(ScheduleDbContext context)
    {
        this.context = context;
        _dbSet = context.Set<T>();
        _connection = context.Database.GetDbConnection();
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> expression)
    {
        return await _dbSet.Where(expression).ToListAsync();
    }

    public async Task<T> GetByIdAsync(Guid id)
    {
        const string sql = @"SELECT * FROM Schedules WHERE Id = @Id";
        return await _connection.QueryFirstOrDefaultAsync<T>(sql, new { Id = id }) ?? throw new KeyNotFoundException();
    }

    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<T> entities)
    {
        await _dbSet.AddRangeAsync(entities);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await context.SaveChangesAsync();
    }

    public async Task RemoveAsync(T entity)
    {
        _dbSet.Remove(entity);
        await context.SaveChangesAsync();
    }
}