using Microsoft.AspNetCore.Mvc;
using WarmachineAPI.Models;
using WarmachineAPI.Services;

namespace WarmachineAPI.Controllers;

public class ArmyPointsSummary
{
    public int TotalPoints { get; set; }
    public int PointLimit { get; set; }
    public int RemainingPoints { get; set; }
    public bool IsOverLimit { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class ArmiesController : ControllerBase
{
    private readonly IRepository<Army> _armies;
    private readonly IRepository<Faction> _factions;
    private readonly IRepository<ArmyEntry> _armyEntries;
    private readonly IRepository<UnitDefinition> _units;

    public ArmiesController(
        IRepository<Army> armies,
        IRepository<Faction> factions,
        IRepository<ArmyEntry> armyEntries,
        IRepository<UnitDefinition> units)
    {
        _armies = armies;
        _factions = factions;
        _armyEntries = armyEntries;
        _units = units;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Army>> GetAll()
    {
        return Ok(_armies.GetAll());
    }

    [HttpGet("{id:guid}")]
    public ActionResult<Army> GetById(Guid id)
    {
        var army = _armies.GetById(id);
        return army is null ? NotFound() : Ok(army);
    }

    [HttpGet("{id:guid}/points-summary")]
    public ActionResult<ArmyPointsSummary> GetPointsSummary(Guid id)
    {
        var army = _armies.GetById(id);
        if (army is null)
        {
            return NotFound();
        }

        var totalPoints = _armyEntries.GetAll()
            .Where(e => e.ArmyId == id)
            .Sum(e => (_units.GetById(e.UnitDefinitionId)?.PointCost ?? 0) * e.Quantity);

        return Ok(new ArmyPointsSummary
        {
            TotalPoints = totalPoints,
            PointLimit = army.PointLimit,
            RemainingPoints = army.PointLimit - totalPoints,
            IsOverLimit = totalPoints > army.PointLimit
        });
    }

    [HttpPost]
    public ActionResult<Army> Create(Army army)
    {
        if (_factions.GetById(army.FactionId) is null)
        {
            return BadRequest($"Faction '{army.FactionId}' does not exist.");
        }

        var created = _armies.Create(army);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, Army army)
    {
        if (_factions.GetById(army.FactionId) is null)
        {
            return BadRequest($"Faction '{army.FactionId}' does not exist.");
        }

        return _armies.Update(id, army) ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        return _armies.Delete(id) ? NoContent() : NotFound();
    }
}
