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

public class ActivationUpdate
{
    public bool HasActivated { get; set; }
}

public class FeatUpdate
{
    public bool HasUsedFeat { get; set; }
}

/// <summary>
/// A physical model on the board during a live <see cref="GameSession"/> - position, facing,
/// damage, status effects, and once-per-round/once-per-game flags (activation, feat). This is
/// live game state, distinct from the catalog data in <see cref="UnitDefinition"/>.
/// </summary>
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

    /// <summary>Lists all model instances currently on the board for a session.</summary>
    [HttpGet("api/sessions/{sessionId:guid}/models")]
    public ActionResult<IEnumerable<ModelInstance>> GetForSession(Guid sessionId)
    {
        if (_sessions.GetById(sessionId) is null)
        {
            return this.ProblemNotFound($"Session '{sessionId}' does not exist.");
        }

        return Ok(_models.GetAll().Where(m => m.GameSessionId == sessionId));
    }

    /// <summary>Spawns a model instance on the board for a given session, from an army entry.</summary>
    [HttpPost("api/sessions/{sessionId:guid}/models")]
    public ActionResult<ModelInstance> Create(Guid sessionId, ModelInstance model)
    {
        if (_sessions.GetById(sessionId) is null)
        {
            return this.ProblemBadRequest($"Session '{sessionId}' does not exist.");
        }

        if (_armyEntries.GetById(model.ArmyEntryId) is null)
        {
            return this.ProblemBadRequest($"Army entry '{model.ArmyEntryId}' does not exist.");
        }

        model.GameSessionId = sessionId;
        var created = _models.Create(model);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Gets a single model instance by id.</summary>
    [HttpGet("api/models/{id:guid}")]
    public ActionResult<ModelInstance> GetById(Guid id)
    {
        var model = _models.GetById(id);
        return model is null ? NotFound() : Ok(model);
    }

    /// <summary>Updates a model's board position and facing. Does not validate movement legality.</summary>
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

    /// <summary>Updates a model's flat damage total, destroyed flag, and status effects.</summary>
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

    /// <summary>
    /// Replaces a model's column-based damage grid (warjacks/warbeasts), e.g. arm/cortex boxes.
    /// Not validated against the unit's damage column template.
    /// </summary>
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

    /// <summary>
    /// Marks whether a model has activated this round. Cleared for every model in a session by
    /// <c>POST /api/sessions/{id}/reset-activations</c> when advancing to a new round.
    /// </summary>
    [HttpPatch("api/models/{id:guid}/activation")]
    public IActionResult UpdateActivation(Guid id, ActivationUpdate update)
    {
        var model = _models.GetById(id);
        if (model is null)
        {
            return NotFound();
        }

        model.HasActivated = update.HasActivated;
        _models.Update(id, model);
        return NoContent();
    }

    /// <summary>
    /// Marks whether a model (typically a warcaster/warlock) has used its once-per-game feat.
    /// Unlike activation, this is never reset automatically - a feat is used once per game, not per round.
    /// </summary>
    [HttpPatch("api/models/{id:guid}/feat")]
    public IActionResult UpdateFeat(Guid id, FeatUpdate update)
    {
        var model = _models.GetById(id);
        if (model is null)
        {
            return NotFound();
        }

        model.HasUsedFeat = update.HasUsedFeat;
        _models.Update(id, model);
        return NoContent();
    }

    /// <summary>Removes a model instance from the board.</summary>
    [HttpDelete("api/models/{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        return _models.Delete(id) ? NoContent() : NotFound();
    }
}
