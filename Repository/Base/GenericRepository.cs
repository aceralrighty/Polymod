using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TBD.Data;
using TBD.Models.Entities;

namespace TBD.Repository.Base;

public class GenericRepository<T>(GenericDatabaseContext context) : IGenericRepository<T>
    where T : GenericEntity
{
    protected readonly DbSet<T>? _dbSet = context.Set<T>();

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
        return await _dbSet.Where(gId => gId.Id.Equals(id)).FirstOrDefaultAsync() ??
               throw new InvalidOperationException();
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