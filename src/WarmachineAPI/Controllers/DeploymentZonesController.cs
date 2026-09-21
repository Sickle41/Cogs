using Microsoft.AspNetCore.Mvc;
using WarmachineAPI.Models;
using WarmachineAPI.Services;

namespace WarmachineAPI.Controllers;

[ApiController]
public class DeploymentZonesController : ControllerBase
{
    private readonly IRepository<DeploymentZone> _zones;
    private readonly IRepository<Map> _maps;

    public DeploymentZonesController(IRepository<DeploymentZone> zones, IRepository<Map> maps)
    {
        _zones = zones;
        _maps = maps;
    }

    [HttpGet("api/maps/{mapId:guid}/deployment-zones")]
    public ActionResult<IEnumerable<DeploymentZone>> GetForMap(Guid mapId)
    {
        if (_maps.GetById(mapId) is null)
        {
            return NotFound($"Map '{mapId}' does not exist.");
        }

        return Ok(_zones.GetAll().Where(z => z.MapId == mapId));
    }

    [HttpPost("api/maps/{mapId:guid}/deployment-zones")]
    public ActionResult<DeploymentZone> Create(Guid mapId, DeploymentZone zone)
    {
        if (_maps.GetById(mapId) is null)
        {
            return BadRequest($"Map '{mapId}' does not exist.");
        }

        zone.MapId = mapId;
        var created = _zones.Create(zone);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("api/deployment-zones/{id:guid}")]
    public ActionResult<DeploymentZone> GetById(Guid id)
    {
        var zone = _zones.GetById(id);
        return zone is null ? NotFound() : Ok(zone);
    }

    [HttpPut("api/deployment-zones/{id:guid}")]
    public IActionResult Update(Guid id, DeploymentZone zone)
    {
        if (_maps.GetById(zone.MapId) is null)
        {
            return BadRequest($"Map '{zone.MapId}' does not exist.");
        }

        return _zones.Update(id, zone) ? NoContent() : NotFound();
    }

    [HttpDelete("api/deployment-zones/{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        return _zones.Delete(id) ? NoContent() : NotFound();
    }
}
