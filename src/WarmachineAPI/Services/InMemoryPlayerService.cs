using System.Collections.Concurrent;
using WarmachineAPI.Models;

namespace WarmachineAPI.Services;

public class InMemoryPlayerService : IPlayerService
{
    private readonly ConcurrentDictionary<Guid, Player> _players = new();

    public IEnumerable<Player> GetAll() => _players.Values;

    public Player? GetById(Guid id) => _players.GetValueOrDefault(id);

    public Player Create(Player player)
    {
        player.Id = Guid.NewGuid();
        _players[player.Id] = player;
        return player;
    }

    public bool Update(Guid id, Player player)
    {
        if (!_players.ContainsKey(id))
        {
            return false;
        }

        player.Id = id;
        _players[id] = player;
        return true;
    }

    public bool Delete(Guid id) => _players.TryRemove(id, out _);
}
