using Microsoft.AspNetCore.Mvc;
using WarmachineAPI.Models;
using WarmachineAPI.Services;

namespace WarmachineAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FactionsController : ControllerBase
{
    private readonly IRepository<Faction> _factions;
    private readonly IRepository<UnitDefinition> _units;
    private readonly IRepository<Army> _armies;

    public FactionsController(
        IRepository<Faction> factions,
        IRepository<UnitDefinition> units,
        IRepository<Army> armies)
    {
        _factions = factions;
        _units = units;
        _armies = armies;
    }

    [HttpGet]
    public ActionResult<PagedResult<Faction>> GetAll(
        [FromQuery] string? search,
        [FromQuery] int? page,
        [FromQuery] int? pageSize)
    {
        var factions = _factions.GetAll();
        if (!string.IsNullOrWhiteSpace(search))
        {
            factions = factions.Where(f => f.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        return Ok(PagedResult<Faction>.Create(factions, page, pageSize));
    }

    [HttpGet("{id:guid}")]
    public ActionResult<Faction> GetById(Guid id)
    {
        var faction = _factions.GetById(id);
        return faction is null ? NotFound() : Ok(faction);
    }

    [HttpPost]
    public ActionResult<Faction> Create(Faction faction)
    {
        var created = _factions.Create(faction);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, Faction faction)
    {
        return _factions.Update(id, faction) ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        if (_units.GetAll().Any(u => u.FactionId == id))
        {
            return this.ProblemConflict($"Faction '{id}' still has unit definitions referencing it.");
        }

        if (_armies.GetAll().Any(a => a.FactionId == id))
        {
            return this.ProblemConflict($"Faction '{id}' still has armies referencing it.");
        }

        return _factions.Delete(id) ? NoContent() : NotFound();
    }
}
