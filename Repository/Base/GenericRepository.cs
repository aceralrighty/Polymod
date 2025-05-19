using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TBD.Data;
using TBD.Models;

namespace TBD.Repository;

public class GenericRepository<T> : IGenericRepository<T> where T : GenericEntity
{
    private readonly GenericDatabaseContext _context;
    protected readonly DbSet<T>? _dbSet;

    protected GenericRepository(GenericDatabaseContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
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
        return await _dbSet.Where(id => true).FirstOrDefaultAsync() ?? throw new InvalidOperationException();
    }

    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<T> entities)
    {
        await _dbSet.AddRangeAsync(entities);
    }

    public void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public void Remove(T entity)
    {
        _dbSet.Remove(entity);
    }
}