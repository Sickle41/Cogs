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

public class ArmyExportEntry
{
    public string UnitName { get; set; } = string.Empty;
    public UnitCategory Category { get; set; }
    public int PointCost { get; set; }
    public int Quantity { get; set; }
    public string? AttachmentName { get; set; }
    public string? BattlegroupCasterName { get; set; }
}

public class ArmyExport
{
    public string ArmyName { get; set; } = string.Empty;
    public string FactionName { get; set; } = string.Empty;
    public int PointLimit { get; set; }
    public int TotalPoints { get; set; }
    public List<ArmyExportEntry> Entries { get; set; } = new();
}

[ApiController]
[Route("api/[controller]")]
public class ArmiesController : ControllerBase
{
    private readonly IRepository<Army> _armies;
    private readonly IRepository<Faction> _factions;
    private readonly IRepository<ArmyEntry> _armyEntries;
    private readonly IRepository<UnitDefinition> _units;
    private readonly IRepository<Account> _accounts;
    private readonly IRepository<GameParticipant> _participants;
    private readonly IRepository<UnitAttachment> _attachments;

    public ArmiesController(
        IRepository<Army> armies,
        IRepository<Faction> factions,
        IRepository<ArmyEntry> armyEntries,
        IRepository<UnitDefinition> units,
        IRepository<Account> accounts,
        IRepository<GameParticipant> participants,
        IRepository<UnitAttachment> attachments)
    {
        _armies = armies;
        _factions = factions;
        _armyEntries = armyEntries;
        _units = units;
        _accounts = accounts;
        _participants = participants;
        _attachments = attachments;
    }

    [HttpGet]
    public ActionResult<PagedResult<Army>> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
    {
        return Ok(PagedResult<Army>.Create(_armies.GetAll(), page, pageSize));
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

    [HttpGet("{id:guid}/export")]
    public ActionResult<ArmyExport> GetExport(Guid id)
    {
        var army = _armies.GetById(id);
        if (army is null)
        {
            return NotFound();
        }

        var faction = _factions.GetById(army.FactionId);
        var entries = _armyEntries.GetAll().Where(e => e.ArmyId == id).ToList();

        var exportEntries = entries.Select(e =>
        {
            var unit = _units.GetById(e.UnitDefinitionId);
            var attachment = e.UnitAttachmentId.HasValue ? _attachments.GetById(e.UnitAttachmentId.Value) : null;
            var casterEntry = e.BattlegroupCasterEntryId.HasValue
                ? entries.FirstOrDefault(x => x.Id == e.BattlegroupCasterEntryId.Value)
                : null;
            var casterUnit = casterEntry is not null ? _units.GetById(casterEntry.UnitDefinitionId) : null;

            return new ArmyExportEntry
            {
                UnitName = unit?.Name ?? "Unknown",
                Category = unit?.Category ?? default,
                PointCost = unit?.PointCost ?? 0,
                Quantity = e.Quantity,
                AttachmentName = attachment?.Name,
                BattlegroupCasterName = casterUnit?.Name
            };
        }).ToList();

        return Ok(new ArmyExport
        {
            ArmyName = army.Name,
            FactionName = faction?.Name ?? "Unknown",
            PointLimit = army.PointLimit,
            TotalPoints = exportEntries.Sum(e => e.PointCost * e.Quantity),
            Entries = exportEntries
        });
    }

    [HttpPost]
    public ActionResult<Army> Create(Army army)
    {
        if (_factions.GetById(army.FactionId) is null)
        {
            return this.ProblemBadRequest($"Faction '{army.FactionId}' does not exist.");
        }

        if (army.OwnerAccountId.HasValue && _accounts.GetById(army.OwnerAccountId.Value) is null)
        {
            return this.ProblemBadRequest($"Account '{army.OwnerAccountId}' does not exist.");
        }

        var created = _armies.Create(army);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, Army army)
    {
        if (_factions.GetById(army.FactionId) is null)
        {
            return this.ProblemBadRequest($"Faction '{army.FactionId}' does not exist.");
        }

        if (army.OwnerAccountId.HasValue && _accounts.GetById(army.OwnerAccountId.Value) is null)
        {
            return this.ProblemBadRequest($"Account '{army.OwnerAccountId}' does not exist.");
        }

        return _armies.Update(id, army) ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        if (_armyEntries.GetAll().Any(e => e.ArmyId == id))
        {
            return this.ProblemConflict($"Army '{id}' still has entries referencing it.");
        }

        if (_participants.GetAll().Any(p => p.ArmyId == id))
        {
            return this.ProblemConflict($"Army '{id}' still has game participants referencing it.");
        }

        return _armies.Delete(id) ? NoContent() : NotFound();
    }
}
