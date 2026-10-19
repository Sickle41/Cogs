using Microsoft.AspNetCore.Mvc;
using WarmachineAPI.Models;
using WarmachineAPI.Services;

namespace WarmachineAPI.Controllers;

/// <summary>
/// Catalog of playable factions (e.g. Cygnar, Khador). This is the root of the unit catalog -
/// every <see cref="UnitDefinition"/> and <see cref="Army"/> references a faction.
/// </summary>
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

    /// <summary>Lists factions, optionally filtered by name and paged.</summary>
    /// <param name="search">Case-insensitive substring match on <see cref="Faction.Name"/>.</param>
    /// <param name="page">1-based page number. Defaults to 1.</param>
    /// <param name="pageSize">Items per page. Defaults to 50, capped at 200.</param>
    [HttpGet]
    [ResponseCache(Duration = 30, VaryByQueryKeys = new[] { "page", "pageSize", "search" })]
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

    /// <summary>Gets a single faction by id.</summary>
    [HttpGet("{id:guid}")]
    public ActionResult<Faction> GetById(Guid id)
    {
        var faction = _factions.GetById(id);
        return faction is null ? NotFound() : Ok(faction);
    }

    /// <summary>Creates a new faction.</summary>
    [HttpPost]
    public ActionResult<Faction> Create(Faction faction)
    {
        var created = _factions.Create(faction);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Replaces a faction's details.</summary>
    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, Faction faction)
    {
        return _factions.Update(id, faction) ? NoContent() : NotFound();
    }

    /// <summary>
    /// Deletes a faction. Fails with 409 Conflict if any unit definitions or armies still
    /// reference it, to avoid orphaning those records.
    /// </summary>
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
