using Core.Entities;
using System.Linq.Expressions;

namespace Core.DataAccess
{
    // Generic repository interface that defines basic CRUD operations for all entities
    public interface IEntityRepository<T> where T : class, IEntity, new()
    {
        // Returns a single entity matching the given filter
        Task<T> GetAsync(Expression<Func<T, bool>> filter);

        // Returns a queryable list of entities, optionally filtered
        Task<IQueryable<T>> GetListAsync(Expression<Func<T, bool>> filter = null);

        // Adds a new entity to the database
        Task AddAsync(T entity);

        // Updates an existing entity in the database
        Task UpdateAsync(T entity);

        // Deletes an entity from the database
        Task DeleteAsync(T entity);
    }
}
