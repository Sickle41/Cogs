using WarmachineAPI.Models;

namespace WarmachineAPI.Services;

public interface IRepository<T> where T : class, IEntity
{
    IEnumerable<T> GetAll();
    T? GetById(Guid id);
    T Create(T entity);
    bool Update(Guid id, T entity);
    bool Delete(Guid id);
}
