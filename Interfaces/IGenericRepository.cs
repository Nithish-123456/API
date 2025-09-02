using MyWebApi.Common;
using System.Linq.Expressions;

namespace MyWebApi.Interfaces;

public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<PagedResult<T>> GetPagedAsync(PaginationParameters parameters);
    Task<PagedResult<T>> GetFilteredAsync(FilterParameters parameters, Expression<Func<T, bool>>? filter = null);
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<int> CountAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
}