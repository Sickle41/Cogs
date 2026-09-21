using Microsoft.AspNetCore.Mvc;
using WarmachineAPI.Models;
using WarmachineAPI.Services;

namespace WarmachineAPI.Controllers;

[ApiController]
public class SpellsController : ControllerBase
{
    private readonly IRepository<Spell> _spells;
    private readonly IRepository<UnitDefinition> _units;

    public SpellsController(IRepository<Spell> spells, IRepository<UnitDefinition> units)
    {
        _spells = spells;
        _units = units;
    }

    [HttpGet("api/units/{unitId:guid}/spells")]
    public ActionResult<IEnumerable<Spell>> GetForUnit(Guid unitId)
    {
        if (_units.GetById(unitId) is null)
        {
            return NotFound($"Unit '{unitId}' does not exist.");
        }

        return Ok(_spells.GetAll().Where(s => s.UnitDefinitionId == unitId));
    }

    [HttpPost("api/units/{unitId:guid}/spells")]
    public ActionResult<Spell> Create(Guid unitId, Spell spell)
    {
        if (_units.GetById(unitId) is null)
        {
            return BadRequest($"Unit '{unitId}' does not exist.");
        }

        spell.UnitDefinitionId = unitId;
        var created = _spells.Create(spell);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("api/spells/{id:guid}")]
    public ActionResult<Spell> GetById(Guid id)
    {
        var spell = _spells.GetById(id);
        return spell is null ? NotFound() : Ok(spell);
    }

    [HttpPut("api/spells/{id:guid}")]
    public IActionResult Update(Guid id, Spell spell)
    {
        if (_units.GetById(spell.UnitDefinitionId) is null)
        {
            return BadRequest($"Unit '{spell.UnitDefinitionId}' does not exist.");
        }

        return _spells.Update(id, spell) ? NoContent() : NotFound();
    }

    [HttpDelete("api/spells/{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        return _spells.Delete(id) ? NoContent() : NotFound();
    }
}
