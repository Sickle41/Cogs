using Microsoft.AspNetCore.Mvc;
using WarmachineAPI.Models;
using WarmachineAPI.Services;

namespace WarmachineAPI.Controllers;

[ApiController]
public class GameParticipantsController : ControllerBase
{
    private readonly IRepository<GameParticipant> _participants;
    private readonly IRepository<GameSession> _sessions;
    private readonly IRepository<Army> _armies;

    public GameParticipantsController(
        IRepository<GameParticipant> participants,
        IRepository<GameSession> sessions,
        IRepository<Army> armies)
    {
        _participants = participants;
        _sessions = sessions;
        _armies = armies;
    }

    [HttpGet("api/sessions/{sessionId:guid}/participants")]
    public ActionResult<IEnumerable<GameParticipant>> GetForSession(Guid sessionId)
    {
        if (_sessions.GetById(sessionId) is null)
        {
            return NotFound($"Session '{sessionId}' does not exist.");
        }

        return Ok(_participants.GetAll().Where(p => p.GameSessionId == sessionId));
    }

    [HttpPost("api/sessions/{sessionId:guid}/participants")]
    public ActionResult<GameParticipant> Create(Guid sessionId, GameParticipant participant)
    {
        if (_sessions.GetById(sessionId) is null)
        {
            return BadRequest($"Session '{sessionId}' does not exist.");
        }

        if (_armies.GetById(participant.ArmyId) is null)
        {
            return BadRequest($"Army '{participant.ArmyId}' does not exist.");
        }

        participant.GameSessionId = sessionId;
        var created = _participants.Create(participant);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("api/participants/{id:guid}")]
    public ActionResult<GameParticipant> GetById(Guid id)
    {
        var participant = _participants.GetById(id);
        return participant is null ? NotFound() : Ok(participant);
    }

    [HttpDelete("api/participants/{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        return _participants.Delete(id) ? NoContent() : NotFound();
    }
}
