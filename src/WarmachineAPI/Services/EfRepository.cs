using Microsoft.EntityFrameworkCore;
using WarmachineAPI.Data;
using WarmachineAPI.Models;

namespace WarmachineAPI.Services;

public class EfRepository<T> : IRepository<T> where T : class, IEntity
{
    private readonly WarmachineDbContext _context;
    private readonly DbSet<T> _set;

    public EfRepository(WarmachineDbContext context)
    {
        _context = context;
        _set = context.Set<T>();
    }

    public IEnumerable<T> GetAll() => _set.AsNoTracking().ToList();

    public T? GetById(Guid id) => _set.AsNoTracking().FirstOrDefault(e => e.Id == id);

    public T Create(T entity)
    {
        entity.Id = Guid.NewGuid();

        if (entity is IAuditable auditable)
        {
            var now = DateTime.UtcNow;
            auditable.CreatedAt = now;
            auditable.UpdatedAt = now;
        }

        if (entity is IConcurrent newConcurrent)
        {
            newConcurrent.ConcurrencyToken = Guid.NewGuid();
        }

        _set.Add(entity);
        _context.SaveChanges();
        return entity;
    }

    public bool Update(Guid id, T entity)
    {
        var existing = _set.Find(id);
        if (existing is null)
        {
            return false;
        }

        entity.Id = id;

        if (entity is IAuditable incomingAuditable && existing is IAuditable existingAuditable)
        {
            incomingAuditable.CreatedAt = existingAuditable.CreatedAt;
            incomingAuditable.UpdatedAt = DateTime.UtcNow;
        }

        if (entity is IConcurrent incomingConcurrent && existing is IConcurrent existingConcurrent)
        {
            if (incomingConcurrent.ConcurrencyToken != existingConcurrent.ConcurrencyToken)
            {
                throw new ConcurrencyConflictException(
                    $"{typeof(T).Name} '{id}' was modified by another request. Re-fetch and try again.");
            }

            incomingConcurrent.ConcurrencyToken = Guid.NewGuid();
        }

        _context.Entry(existing).CurrentValues.SetValues(entity);
        _context.SaveChanges();
        return true;
    }

    public bool Delete(Guid id)
    {
        var existing = _set.Find(id);
        if (existing is null)
        {
            return false;
        }

        _set.Remove(existing);
        _context.SaveChanges();
        return true;
    }
}
