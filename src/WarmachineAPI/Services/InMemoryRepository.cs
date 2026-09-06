using System.Collections.Concurrent;
using WarmachineAPI.Models;

namespace WarmachineAPI.Services;

public class InMemoryRepository<T> : IRepository<T> where T : class, IEntity
{
    private readonly ConcurrentDictionary<Guid, T> _items = new();

    public IEnumerable<T> GetAll() => _items.Values;

    public T? GetById(Guid id) => _items.GetValueOrDefault(id);

    public T Create(T entity)
    {
        entity.Id = Guid.NewGuid();
        _items[entity.Id] = entity;
        return entity;
    }

    public bool Update(Guid id, T entity)
    {
        if (!_items.ContainsKey(id))
        {
            return false;
        }

        entity.Id = id;
        _items[id] = entity;
        return true;
    }

    public bool Delete(Guid id) => _items.TryRemove(id, out _);
}
