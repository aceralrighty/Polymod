using System.Linq.Expressions;
using TBD.Models.Entities;

namespace TBD.Repository.Base;

public interface IGenericRepository<T> where T : GenericEntity 
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> expression);
    Task<T> GetByIdAsync(Guid id);
    Task AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    Task UpdateAsync(T entity);
    Task RemoveAsync(T entity);
}