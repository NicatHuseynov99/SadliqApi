using Core.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Core.DataAccess.EntityFramework
{
    // Generic base class for Entity Framework repositories
    public class EfEntityRepositoryBase<TEntity, TContext> : IEntityRepository<TEntity>
        where TEntity : class, IEntity, new()
        where TContext : DbContext
    {
        protected readonly TContext _context;

        public EfEntityRepositoryBase(TContext context)
        {
            _context = context;
        }

        // Adds a new entity and saves changes to the database.
        // Add() marks the whole object graph, so child collections (features, images,
        // menu items) are inserted together with the parent.
        public async Task AddAsync(TEntity entity)
        {
            await _context.Set<TEntity>().AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        // Silmə tələbi. ISoftDelete olan sətirlər bazadan çıxarılmır — yalnız
        // işarələnir, beləliklə tarixçə qalır və geri qaytarmaq mümkün olur.
        // Qalanları (əlaqə cədvəlləri kimi) həqiqətən silinir.
        public async Task DeleteAsync(TEntity entity)
        {
            if (entity is ISoftDelete soft)
            {
                soft.IsDeleted = true;
                soft.DeletedAt = DateTime.UtcNow;
                _context.Entry(entity).State = EntityState.Modified;
            }
            else
            {
                _context.Entry(entity).State = EntityState.Deleted;
            }

            await _context.SaveChangesAsync();
        }

        // Returns a single entity matching the given filter expression
        public async Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> filter)
        {
            return await _context.Set<TEntity>().SingleOrDefaultAsync(filter);
        }

        // Returns a queryable list of entities, optionally filtered
        public async Task<IQueryable<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> filter = null)
        {
            var query = filter == null
                ? _context.Set<TEntity>().AsNoTracking()
                : _context.Set<TEntity>().Where(filter).AsNoTracking();

            return await Task.FromResult(query);
        }

        // Marks entity as modified and saves changes to the database
        public async Task UpdateAsync(TEntity entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }
    }
}
