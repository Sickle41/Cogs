using Microsoft.AspNetCore.Mvc;
using WarmachineAPI.Models;
using WarmachineAPI.Services;

namespace WarmachineAPI.Controllers;

[ApiController]
public class FeatsController : ControllerBase
{
    private readonly IRepository<Feat> _feats;
    private readonly IRepository<UnitDefinition> _units;

    public FeatsController(IRepository<Feat> feats, IRepository<UnitDefinition> units)
    {
        _feats = feats;
        _units = units;
    }

    [HttpGet("api/units/{unitId:guid}/feats")]
    public ActionResult<IEnumerable<Feat>> GetForUnit(Guid unitId)
    {
        if (_units.GetById(unitId) is null)
        {
            return this.ProblemNotFound($"Unit '{unitId}' does not exist.");
        }

        return Ok(_feats.GetAll().Where(f => f.UnitDefinitionId == unitId));
    }

    [HttpPost("api/units/{unitId:guid}/feats")]
    public ActionResult<Feat> Create(Guid unitId, Feat feat)
    {
        if (_units.GetById(unitId) is null)
        {
            return this.ProblemBadRequest($"Unit '{unitId}' does not exist.");
        }

        feat.UnitDefinitionId = unitId;
        var created = _feats.Create(feat);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("api/feats/{id:guid}")]
    public ActionResult<Feat> GetById(Guid id)
    {
        var feat = _feats.GetById(id);
        return feat is null ? NotFound() : Ok(feat);
    }

    [HttpPut("api/feats/{id:guid}")]
    public IActionResult Update(Guid id, Feat feat)
    {
        if (_units.GetById(feat.UnitDefinitionId) is null)
        {
            return this.ProblemBadRequest($"Unit '{feat.UnitDefinitionId}' does not exist.");
        }

        return _feats.Update(id, feat) ? NoContent() : NotFound();
    }

    [HttpDelete("api/feats/{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        return _feats.Delete(id) ? NoContent() : NotFound();
    }
}
