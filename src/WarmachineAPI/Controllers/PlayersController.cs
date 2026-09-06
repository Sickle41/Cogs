using Microsoft.AspNetCore.Mvc;
using WarmachineAPI.Models;
using WarmachineAPI.Services;

namespace WarmachineAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlayersController : ControllerBase
{
    private readonly IPlayerService _playerService;

    public PlayersController(IPlayerService playerService)
    {
        _playerService = playerService;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Player>> GetAll()
    {
        return Ok(_playerService.GetAll());
    }

    [HttpGet("{id:guid}")]
    public ActionResult<Player> GetById(Guid id)
    {
        var player = _playerService.GetById(id);
        return player is null ? NotFound() : Ok(player);
    }

    [HttpPost]
    public ActionResult<Player> Create(Player player)
    {
        var created = _playerService.Create(player);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, Player player)
    {
        return _playerService.Update(id, player) ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        return _playerService.Delete(id) ? NoContent() : NotFound();
    }
}
