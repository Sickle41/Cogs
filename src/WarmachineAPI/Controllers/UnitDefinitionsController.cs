using Microsoft.AspNetCore.Mvc;
using WarmachineAPI.Models;
using WarmachineAPI.Services;

namespace WarmachineAPI.Controllers;

[ApiController]
[Route("api/units")]
public class UnitDefinitionsController : ControllerBase
{
    private readonly IRepository<UnitDefinition> _units;
    private readonly IRepository<Faction> _factions;

    public UnitDefinitionsController(IRepository<UnitDefinition> units, IRepository<Faction> factions)
    {
        _units = units;
        _factions = factions;
    }

    [HttpGet]
    public ActionResult<IEnumerable<UnitDefinition>> GetAll([FromQuery] Guid? factionId)
    {
        var units = _units.GetAll();
        if (factionId.HasValue)
        {
            units = units.Where(u => u.FactionId == factionId.Value);
        }

        return Ok(units);
    }

    [HttpGet("{id:guid}")]
    public ActionResult<UnitDefinition> GetById(Guid id)
    {
        var unit = _units.GetById(id);
        return unit is null ? NotFound() : Ok(unit);
    }

    [HttpPost]
    public ActionResult<UnitDefinition> Create(UnitDefinition unit)
    {
        if (_factions.GetById(unit.FactionId) is null)
        {
            return BadRequest($"Faction '{unit.FactionId}' does not exist.");
        }

        var created = _units.Create(unit);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, UnitDefinition unit)
    {
        if (_factions.GetById(unit.FactionId) is null)
        {
            return BadRequest($"Faction '{unit.FactionId}' does not exist.");
        }

        return _units.Update(id, unit) ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        return _units.Delete(id) ? NoContent() : NotFound();
    }
}
