using Microsoft.AspNetCore.Mvc;
using WarmachineAPI.Models;
using WarmachineAPI.Services;

namespace WarmachineAPI.Controllers;

[ApiController]
public class ArmyEntriesController : ControllerBase
{
    private readonly IRepository<ArmyEntry> _entries;
    private readonly IRepository<Army> _armies;
    private readonly IRepository<UnitDefinition> _units;

    public ArmyEntriesController(
        IRepository<ArmyEntry> entries,
        IRepository<Army> armies,
        IRepository<UnitDefinition> units)
    {
        _entries = entries;
        _armies = armies;
        _units = units;
    }

    [HttpGet("api/armies/{armyId:guid}/entries")]
    public ActionResult<IEnumerable<ArmyEntry>> GetForArmy(Guid armyId)
    {
        if (_armies.GetById(armyId) is null)
        {
            return NotFound($"Army '{armyId}' does not exist.");
        }

        return Ok(_entries.GetAll().Where(e => e.ArmyId == armyId));
    }

    [HttpPost("api/armies/{armyId:guid}/entries")]
    public ActionResult<ArmyEntry> Create(Guid armyId, ArmyEntry entry)
    {
        var army = _armies.GetById(armyId);
        if (army is null)
        {
            return BadRequest($"Army '{armyId}' does not exist.");
        }

        var unit = _units.GetById(entry.UnitDefinitionId);
        if (unit is null)
        {
            return BadRequest($"Unit '{entry.UnitDefinitionId}' does not exist.");
        }

        if (unit.FactionId != army.FactionId)
        {
            return BadRequest($"Unit '{unit.Name}' belongs to a different faction than army '{army.Name}'.");
        }

        entry.ArmyId = armyId;
        var created = _entries.Create(entry);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("api/army-entries/{id:guid}")]
    public ActionResult<ArmyEntry> GetById(Guid id)
    {
        var entry = _entries.GetById(id);
        return entry is null ? NotFound() : Ok(entry);
    }

    [HttpPut("api/army-entries/{id:guid}")]
    public IActionResult Update(Guid id, ArmyEntry entry)
    {
        var army = _armies.GetById(entry.ArmyId);
        if (army is null)
        {
            return BadRequest($"Army '{entry.ArmyId}' does not exist.");
        }

        var unit = _units.GetById(entry.UnitDefinitionId);
        if (unit is null)
        {
            return BadRequest($"Unit '{entry.UnitDefinitionId}' does not exist.");
        }

        if (unit.FactionId != army.FactionId)
        {
            return BadRequest($"Unit '{unit.Name}' belongs to a different faction than army '{army.Name}'.");
        }

        return _entries.Update(id, entry) ? NoContent() : NotFound();
    }

    [HttpDelete("api/army-entries/{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        return _entries.Delete(id) ? NoContent() : NotFound();
    }
}
