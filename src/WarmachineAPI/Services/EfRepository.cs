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
