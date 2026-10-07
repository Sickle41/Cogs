using Microsoft.AspNetCore.Mvc;
using WarmachineAPI.Models;
using WarmachineAPI.Services;

namespace WarmachineAPI.Controllers;

[ApiController]
public class UnitAttachmentsController : ControllerBase
{
    private readonly IRepository<UnitAttachment> _attachments;
    private readonly IRepository<UnitDefinition> _units;

    public UnitAttachmentsController(IRepository<UnitAttachment> attachments, IRepository<UnitDefinition> units)
    {
        _attachments = attachments;
        _units = units;
    }

    [HttpGet("api/units/{unitId:guid}/attachments")]
    public ActionResult<IEnumerable<UnitAttachment>> GetForUnit(Guid unitId)
    {
        if (_units.GetById(unitId) is null)
        {
            return this.ProblemNotFound($"Unit '{unitId}' does not exist.");
        }

        return Ok(_attachments.GetAll().Where(a => a.UnitDefinitionId == unitId));
    }

    [HttpPost("api/units/{unitId:guid}/attachments")]
    public ActionResult<UnitAttachment> Create(Guid unitId, UnitAttachment attachment)
    {
        if (_units.GetById(unitId) is null)
        {
            return this.ProblemBadRequest($"Unit '{unitId}' does not exist.");
        }

        attachment.UnitDefinitionId = unitId;
        var created = _attachments.Create(attachment);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("api/unit-attachments/{id:guid}")]
    public ActionResult<UnitAttachment> GetById(Guid id)
    {
        var attachment = _attachments.GetById(id);
        return attachment is null ? NotFound() : Ok(attachment);
    }

    [HttpPut("api/unit-attachments/{id:guid}")]
    public IActionResult Update(Guid id, UnitAttachment attachment)
    {
        if (_units.GetById(attachment.UnitDefinitionId) is null)
        {
            return this.ProblemBadRequest($"Unit '{attachment.UnitDefinitionId}' does not exist.");
        }

        return _attachments.Update(id, attachment) ? NoContent() : NotFound();
    }

    [HttpDelete("api/unit-attachments/{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        return _attachments.Delete(id) ? NoContent() : NotFound();
    }
}
