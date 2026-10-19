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
    private readonly IRepository<ArmyEntry> _armyEntries;
    private readonly IRepository<Spell> _spells;
    private readonly IRepository<Ability> _abilities;
    private readonly IRepository<UnitAttachment> _attachments;
    private readonly IRepository<Feat> _feats;

    public UnitDefinitionsController(
        IRepository<UnitDefinition> units,
        IRepository<Faction> factions,
        IRepository<ArmyEntry> armyEntries,
        IRepository<Spell> spells,
        IRepository<Ability> abilities,
        IRepository<UnitAttachment> attachments,
        IRepository<Feat> feats)
    {
        _units = units;
        _factions = factions;
        _armyEntries = armyEntries;
        _spells = spells;
        _abilities = abilities;
        _attachments = attachments;
        _feats = feats;
    }

    [HttpGet]
    public ActionResult<PagedResult<UnitDefinition>> GetAll(
        [FromQuery] Guid? factionId,
        [FromQuery] string? search,
        [FromQuery] int? page,
        [FromQuery] int? pageSize)
    {
        var units = _units.GetAll();
        if (factionId.HasValue)
        {
            units = units.Where(u => u.FactionId == factionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            units = units.Where(u => u.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        return Ok(PagedResult<UnitDefinition>.Create(units, page, pageSize));
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
            return this.ProblemBadRequest($"Faction '{unit.FactionId}' does not exist.");
        }

        var created = _units.Create(unit);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, UnitDefinition unit)
    {
        if (_factions.GetById(unit.FactionId) is null)
        {
            return this.ProblemBadRequest($"Faction '{unit.FactionId}' does not exist.");
        }

        return _units.Update(id, unit) ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        if (_armyEntries.GetAll().Any(e => e.UnitDefinitionId == id))
        {
            return this.ProblemConflict($"Unit '{id}' still has army entries referencing it.");
        }

        if (_spells.GetAll().Any(s => s.UnitDefinitionId == id))
        {
            return this.ProblemConflict($"Unit '{id}' still has spells referencing it.");
        }

        if (_abilities.GetAll().Any(a => a.UnitDefinitionId == id))
        {
            return this.ProblemConflict($"Unit '{id}' still has abilities referencing it.");
        }

        if (_attachments.GetAll().Any(a => a.UnitDefinitionId == id))
        {
            return this.ProblemConflict($"Unit '{id}' still has unit attachments referencing it.");
        }

        if (_feats.GetAll().Any(f => f.UnitDefinitionId == id))
        {
            return this.ProblemConflict($"Unit '{id}' still has feats referencing it.");
        }

        return _units.Delete(id) ? NoContent() : NotFound();
    }
}
