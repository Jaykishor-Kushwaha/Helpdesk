using Helpdesk.Models;
using System.Linq.Expressions;

namespace Helpdesk.Interfaces
{
    public interface IGenericRepository<T> where T : class, IEntity
    {
        IQueryable<T> Query();

        Task<IEnumerable<T>> GetAllAsync();

        Task<T?> GetByIdAsync(int id);

        Task<T?> GetByIdWithIncludeAsync(
            int id,
            params Expression<Func<T, object>>[] includes);

        Task<IEnumerable<T>> GetByConditionAsync(
            Expression<Func<T, bool>> predicate);

        Task<IEnumerable<T>> GetByQueryAsync(
            Func<IQueryable<T>, IQueryable<T>> queryFunc);

        Task AddAsync(T entity);

        Task UpdateAsync(T entity);

        Task DeleteAsync(T entity);

        Task<bool> ExistsAsync(int id);

        Task<int> CountAsync();

        Task<int> CountAsync(Expression<Func<T, bool>> predicate);

        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
    }
}