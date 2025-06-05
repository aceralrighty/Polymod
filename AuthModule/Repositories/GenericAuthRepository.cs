using System.Data;
using System.Linq.Expressions;
using Dapper;
using Microsoft.EntityFrameworkCore;
using TBD.AuthModule.Data;

namespace TBD.AuthModule.Repositories;

public class GenericAuthRepository<T> where T : class
{
    private readonly AuthDbContext _context;
    protected readonly DbSet<T> _dbSet;
    private readonly IDbConnection _dbConnection;

    protected GenericAuthRepository(AuthDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
        _dbConnection = context.Database.GetDbConnection();
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public async Task<T> GetByIdAsync(Guid id)
    {
        const string sql = @"SELECT * FROM AuthUsers WHERE Id = @Id";
        return await _dbConnection.QueryFirstOrDefaultAsync<T>(sql, new { Id = id }) ??
               throw new KeyNotFoundException();
    }

    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
