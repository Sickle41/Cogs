using Microsoft.AspNetCore.Mvc;
using WarmachineAPI.Models;
using WarmachineAPI.Services;

namespace WarmachineAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MapsController : ControllerBase
{
    private readonly IRepository<Map> _maps;
    private readonly IRepository<TerrainFeature> _terrain;
    private readonly IRepository<DeploymentZone> _deploymentZones;
    private readonly IRepository<GameSession> _sessions;

    public MapsController(
        IRepository<Map> maps,
        IRepository<TerrainFeature> terrain,
        IRepository<DeploymentZone> deploymentZones,
        IRepository<GameSession> sessions)
    {
        _maps = maps;
        _terrain = terrain;
        _deploymentZones = deploymentZones;
        _sessions = sessions;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Map>> GetAll()
    {
        return Ok(_maps.GetAll());
    }

    [HttpGet("{id:guid}")]
    public ActionResult<Map> GetById(Guid id)
    {
        var map = _maps.GetById(id);
        return map is null ? NotFound() : Ok(map);
    }

    [HttpPost]
    public ActionResult<Map> Create(Map map)
    {
        var created = _maps.Create(map);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, Map map)
    {
        return _maps.Update(id, map) ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        if (_terrain.GetAll().Any(t => t.MapId == id))
        {
            return this.ProblemConflict($"Map '{id}' still has terrain features referencing it.");
        }

        if (_deploymentZones.GetAll().Any(z => z.MapId == id))
        {
            return this.ProblemConflict($"Map '{id}' still has deployment zones referencing it.");
        }

        if (_sessions.GetAll().Any(s => s.MapId == id))
        {
            return this.ProblemConflict($"Map '{id}' still has game sessions referencing it.");
        }

        return _maps.Delete(id) ? NoContent() : NotFound();
    }
}
