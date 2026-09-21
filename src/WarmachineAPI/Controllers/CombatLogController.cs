using Microsoft.AspNetCore.Mvc;
using WarmachineAPI.Models;
using WarmachineAPI.Services;

namespace WarmachineAPI.Controllers;

[ApiController]
public class CombatLogController : ControllerBase
{
    private readonly IRepository<CombatLogEntry> _entries;
    private readonly IRepository<GameSession> _sessions;
    private readonly IRepository<ModelInstance> _models;

    public CombatLogController(
        IRepository<CombatLogEntry> entries,
        IRepository<GameSession> sessions,
        IRepository<ModelInstance> models)
    {
        _entries = entries;
        _sessions = sessions;
        _models = models;
    }

    [HttpGet("api/sessions/{sessionId:guid}/log")]
    public ActionResult<IEnumerable<CombatLogEntry>> GetForSession(Guid sessionId)
    {
        if (_sessions.GetById(sessionId) is null)
        {
            return NotFound($"Session '{sessionId}' does not exist.");
        }

        return Ok(_entries.GetAll()
            .Where(e => e.GameSessionId == sessionId)
            .OrderBy(e => e.CreatedAt));
    }

    [HttpPost("api/sessions/{sessionId:guid}/log")]
    public ActionResult<CombatLogEntry> Create(Guid sessionId, CombatLogEntry entry)
    {
        if (_sessions.GetById(sessionId) is null)
        {
            return BadRequest($"Session '{sessionId}' does not exist.");
        }

        if (entry.ActorModelInstanceId.HasValue && _models.GetById(entry.ActorModelInstanceId.Value) is null)
        {
            return BadRequest($"Actor model instance '{entry.ActorModelInstanceId}' does not exist.");
        }

        if (entry.TargetModelInstanceId.HasValue && _models.GetById(entry.TargetModelInstanceId.Value) is null)
        {
            return BadRequest($"Target model instance '{entry.TargetModelInstanceId}' does not exist.");
        }

        entry.GameSessionId = sessionId;
        entry.CreatedAt = DateTime.UtcNow;
        var created = _entries.Create(entry);
        return CreatedAtAction(nameof(GetForSession), new { sessionId }, created);
    }
}
