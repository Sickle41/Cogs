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
    private readonly IRepository<UnitAttachment> _attachments;

    public ArmyEntriesController(
        IRepository<ArmyEntry> entries,
        IRepository<Army> armies,
        IRepository<UnitDefinition> units,
        IRepository<UnitAttachment> attachments)
    {
        _entries = entries;
        _armies = armies;
        _units = units;
        _attachments = attachments;
    }

    private ActionResult? ValidateUnitAttachment(ArmyEntry entry)
    {
        if (!entry.UnitAttachmentId.HasValue)
        {
            return null;
        }

        var attachment = _attachments.GetById(entry.UnitAttachmentId.Value);
        if (attachment is null)
        {
            return this.ProblemBadRequest($"Unit attachment '{entry.UnitAttachmentId}' does not exist.");
        }

        if (attachment.UnitDefinitionId != entry.UnitDefinitionId)
        {
            return this.ProblemBadRequest(
                $"Unit attachment '{attachment.Name}' does not belong to unit '{entry.UnitDefinitionId}'.");
        }

        return null;
    }

    [HttpGet("api/armies/{armyId:guid}/entries")]
    public ActionResult<IEnumerable<ArmyEntry>> GetForArmy(Guid armyId)
    {
        if (_armies.GetById(armyId) is null)
        {
            return this.ProblemNotFound($"Army '{armyId}' does not exist.");
        }

        return Ok(_entries.GetAll().Where(e => e.ArmyId == armyId));
    }

    [HttpPost("api/armies/{armyId:guid}/entries")]
    public ActionResult<ArmyEntry> Create(Guid armyId, ArmyEntry entry)
    {
        var army = _armies.GetById(armyId);
        if (army is null)
        {
            return this.ProblemBadRequest($"Army '{armyId}' does not exist.");
        }

        var unit = _units.GetById(entry.UnitDefinitionId);
        if (unit is null)
        {
            return this.ProblemBadRequest($"Unit '{entry.UnitDefinitionId}' does not exist.");
        }

        if (unit.FactionId != army.FactionId)
        {
            return this.ProblemBadRequest($"Unit '{unit.Name}' belongs to a different faction than army '{army.Name}'.");
        }

        if (ValidateUnitAttachment(entry) is ActionResult attachmentError)
        {
            return attachmentError;
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
            return this.ProblemBadRequest($"Army '{entry.ArmyId}' does not exist.");
        }

        var unit = _units.GetById(entry.UnitDefinitionId);
        if (unit is null)
        {
            return this.ProblemBadRequest($"Unit '{entry.UnitDefinitionId}' does not exist.");
        }

        if (unit.FactionId != army.FactionId)
        {
            return this.ProblemBadRequest($"Unit '{unit.Name}' belongs to a different faction than army '{army.Name}'.");
        }

        if (ValidateUnitAttachment(entry) is ActionResult attachmentError)
        {
            return attachmentError;
        }

        return _entries.Update(id, entry) ? NoContent() : NotFound();
    }

    [HttpDelete("api/army-entries/{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        return _entries.Delete(id) ? NoContent() : NotFound();
    }
}
