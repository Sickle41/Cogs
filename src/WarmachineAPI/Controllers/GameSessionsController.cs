using Microsoft.AspNetCore.Mvc;
using WarmachineAPI.Models;
using WarmachineAPI.Services;

namespace WarmachineAPI.Controllers;

[ApiController]
[Route("api/sessions")]
public class GameSessionsController : ControllerBase
{
    private readonly IRepository<GameSession> _sessions;
    private readonly IRepository<Map> _maps;

    public GameSessionsController(IRepository<GameSession> sessions, IRepository<Map> maps)
    {
        _sessions = sessions;
        _maps = maps;
    }

    [HttpGet]
    public ActionResult<IEnumerable<GameSession>> GetAll()
    {
        return Ok(_sessions.GetAll());
    }

    [HttpGet("{id:guid}")]
    public ActionResult<GameSession> GetById(Guid id)
    {
        var session = _sessions.GetById(id);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpPost]
    public ActionResult<GameSession> Create(GameSession session)
    {
        if (_maps.GetById(session.MapId) is null)
        {
            return BadRequest($"Map '{session.MapId}' does not exist.");
        }

        var created = _sessions.Create(session);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, GameSession session)
    {
        if (_maps.GetById(session.MapId) is null)
        {
            return BadRequest($"Map '{session.MapId}' does not exist.");
        }

        return _sessions.Update(id, session) ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        return _sessions.Delete(id) ? NoContent() : NotFound();
    }
}
