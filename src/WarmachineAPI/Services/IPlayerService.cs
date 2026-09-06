using WarmachineAPI.Models;

namespace WarmachineAPI.Services;

public interface IPlayerService
{
    IEnumerable<Player> GetAll();
    Player? GetById(Guid id);
    Player Create(Player player);
    bool Update(Guid id, Player player);
    bool Delete(Guid id);
}
