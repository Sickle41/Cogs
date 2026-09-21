using Microsoft.AspNetCore.Mvc;
using WarmachineAPI.Models;
using WarmachineAPI.Services;

namespace WarmachineAPI.Controllers;

public class PositionUpdate
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Facing { get; set; }
}

public class DamageUpdate
{
    public int DamageTaken { get; set; }
    public bool IsDestroyed { get; set; }
    public List<StatusEffect> StatusEffects { get; set; } = new();
}

[ApiController]
public class ModelInstancesController : ControllerBase
{
    private readonly IRepository<ModelInstance> _models;
    private readonly IRepository<GameSession> _sessions;
    private readonly IRepository<ArmyEntry> _armyEntries;

    public ModelInstancesController(
        IRepository<ModelInstance> models,
        IRepository<GameSession> sessions,
        IRepository<ArmyEntry> armyEntries)
    {
        _models = models;
        _sessions = sessions;
        _armyEntries = armyEntries;
    }

    [HttpGet("api/sessions/{sessionId:guid}/models")]
    public ActionResult<IEnumerable<ModelInstance>> GetForSession(Guid sessionId)
    {
        if (_sessions.GetById(sessionId) is null)
        {
            return NotFound($"Session '{sessionId}' does not exist.");
        }

        return Ok(_models.GetAll().Where(m => m.GameSessionId == sessionId));
    }

    [HttpPost("api/sessions/{sessionId:guid}/models")]
    public ActionResult<ModelInstance> Create(Guid sessionId, ModelInstance model)
    {
        if (_sessions.GetById(sessionId) is null)
        {
            return BadRequest($"Session '{sessionId}' does not exist.");
        }

        if (_armyEntries.GetById(model.ArmyEntryId) is null)
        {
            return BadRequest($"Army entry '{model.ArmyEntryId}' does not exist.");
        }

        model.GameSessionId = sessionId;
        var created = _models.Create(model);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("api/models/{id:guid}")]
    public ActionResult<ModelInstance> GetById(Guid id)
    {
        var model = _models.GetById(id);
        return model is null ? NotFound() : Ok(model);
    }

    [HttpPatch("api/models/{id:guid}/position")]
    public IActionResult UpdatePosition(Guid id, PositionUpdate update)
    {
        var model = _models.GetById(id);
        if (model is null)
        {
            return NotFound();
        }

        model.X = update.X;
        model.Y = update.Y;
        model.Facing = update.Facing;
        _models.Update(id, model);
        return NoContent();
    }

    [HttpPatch("api/models/{id:guid}/damage")]
    public IActionResult UpdateDamage(Guid id, DamageUpdate update)
    {
        var model = _models.GetById(id);
        if (model is null)
        {
            return NotFound();
        }

        model.DamageTaken = update.DamageTaken;
        model.IsDestroyed = update.IsDestroyed;
        model.StatusEffects = update.StatusEffects;
        _models.Update(id, model);
        return NoContent();
    }

    [HttpPatch("api/models/{id:guid}/damage-grid")]
    public IActionResult UpdateDamageGrid(Guid id, List<DamageColumnState> update)
    {
        var model = _models.GetById(id);
        if (model is null)
        {
            return NotFound();
        }

        model.DamageGrid = update;
        _models.Update(id, model);
        return NoContent();
    }

    [HttpDelete("api/models/{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        return _models.Delete(id) ? NoContent() : NotFound();
    }
}
