using Microsoft.AspNetCore.Mvc;
using WarmachineAPI.Models;
using WarmachineAPI.Services;

namespace WarmachineAPI.Controllers;

[ApiController]
public class ControlZonesController : ControllerBase
{
    private readonly IRepository<ControlZone> _zones;
    private readonly IRepository<Scenario> _scenarios;

    public ControlZonesController(IRepository<ControlZone> zones, IRepository<Scenario> scenarios)
    {
        _zones = zones;
        _scenarios = scenarios;
    }

    [HttpGet("api/scenarios/{scenarioId:guid}/zones")]
    public ActionResult<IEnumerable<ControlZone>> GetForScenario(Guid scenarioId)
    {
        if (_scenarios.GetById(scenarioId) is null)
        {
            return NotFound($"Scenario '{scenarioId}' does not exist.");
        }

        return Ok(_zones.GetAll().Where(z => z.ScenarioId == scenarioId));
    }

    [HttpPost("api/scenarios/{scenarioId:guid}/zones")]
    public ActionResult<ControlZone> Create(Guid scenarioId, ControlZone zone)
    {
        if (_scenarios.GetById(scenarioId) is null)
        {
            return BadRequest($"Scenario '{scenarioId}' does not exist.");
        }

        zone.ScenarioId = scenarioId;
        var created = _zones.Create(zone);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("api/control-zones/{id:guid}")]
    public ActionResult<ControlZone> GetById(Guid id)
    {
        var zone = _zones.GetById(id);
        return zone is null ? NotFound() : Ok(zone);
    }

    [HttpPut("api/control-zones/{id:guid}")]
    public IActionResult Update(Guid id, ControlZone zone)
    {
        if (_scenarios.GetById(zone.ScenarioId) is null)
        {
            return BadRequest($"Scenario '{zone.ScenarioId}' does not exist.");
        }

        return _zones.Update(id, zone) ? NoContent() : NotFound();
    }

    [HttpDelete("api/control-zones/{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        return _zones.Delete(id) ? NoContent() : NotFound();
    }
}
