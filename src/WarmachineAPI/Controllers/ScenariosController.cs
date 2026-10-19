using Microsoft.AspNetCore.Mvc;
using WarmachineAPI.Models;
using WarmachineAPI.Services;

namespace WarmachineAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScenariosController : ControllerBase
{
    private readonly IRepository<Scenario> _scenarios;
    private readonly IRepository<ControlZone> _controlZones;
    private readonly IRepository<GameSession> _sessions;

    public ScenariosController(
        IRepository<Scenario> scenarios,
        IRepository<ControlZone> controlZones,
        IRepository<GameSession> sessions)
    {
        _scenarios = scenarios;
        _controlZones = controlZones;
        _sessions = sessions;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Scenario>> GetAll()
    {
        return Ok(_scenarios.GetAll());
    }

    [HttpGet("{id:guid}")]
    public ActionResult<Scenario> GetById(Guid id)
    {
        var scenario = _scenarios.GetById(id);
        return scenario is null ? NotFound() : Ok(scenario);
    }

    [HttpPost]
    public ActionResult<Scenario> Create(Scenario scenario)
    {
        var created = _scenarios.Create(scenario);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, Scenario scenario)
    {
        return _scenarios.Update(id, scenario) ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        if (_controlZones.GetAll().Any(z => z.ScenarioId == id))
        {
            return this.ProblemConflict($"Scenario '{id}' still has control zones referencing it.");
        }

        if (_sessions.GetAll().Any(s => s.ScenarioId == id))
        {
            return this.ProblemConflict($"Scenario '{id}' still has game sessions referencing it.");
        }

        return _scenarios.Delete(id) ? NoContent() : NotFound();
    }
}
