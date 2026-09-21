using Microsoft.AspNetCore.Mvc;
using WarmachineAPI.Models;
using WarmachineAPI.Services;

namespace WarmachineAPI.Controllers;

[ApiController]
public class AbilitiesController : ControllerBase
{
    private readonly IRepository<Ability> _abilities;
    private readonly IRepository<UnitDefinition> _units;

    public AbilitiesController(IRepository<Ability> abilities, IRepository<UnitDefinition> units)
    {
        _abilities = abilities;
        _units = units;
    }

    [HttpGet("api/units/{unitId:guid}/abilities")]
    public ActionResult<IEnumerable<Ability>> GetForUnit(Guid unitId)
    {
        if (_units.GetById(unitId) is null)
        {
            return NotFound($"Unit '{unitId}' does not exist.");
        }

        return Ok(_abilities.GetAll().Where(a => a.UnitDefinitionId == unitId));
    }

    [HttpPost("api/units/{unitId:guid}/abilities")]
    public ActionResult<Ability> Create(Guid unitId, Ability ability)
    {
        if (_units.GetById(unitId) is null)
        {
            return BadRequest($"Unit '{unitId}' does not exist.");
        }

        ability.UnitDefinitionId = unitId;
        var created = _abilities.Create(ability);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("api/abilities/{id:guid}")]
    public ActionResult<Ability> GetById(Guid id)
    {
        var ability = _abilities.GetById(id);
        return ability is null ? NotFound() : Ok(ability);
    }

    [HttpPut("api/abilities/{id:guid}")]
    public IActionResult Update(Guid id, Ability ability)
    {
        if (_units.GetById(ability.UnitDefinitionId) is null)
        {
            return BadRequest($"Unit '{ability.UnitDefinitionId}' does not exist.");
        }

        return _abilities.Update(id, ability) ? NoContent() : NotFound();
    }

    [HttpDelete("api/abilities/{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        return _abilities.Delete(id) ? NoContent() : NotFound();
    }
}
