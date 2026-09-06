using Microsoft.AspNetCore.Mvc;
using WarmachineAPI.Models;
using WarmachineAPI.Services;

namespace WarmachineAPI.Controllers;

[ApiController]
public class TerrainController : ControllerBase
{
    private readonly IRepository<TerrainFeature> _terrain;
    private readonly IRepository<Map> _maps;

    public TerrainController(IRepository<TerrainFeature> terrain, IRepository<Map> maps)
    {
        _terrain = terrain;
        _maps = maps;
    }

    [HttpGet("api/maps/{mapId:guid}/terrain")]
    public ActionResult<IEnumerable<TerrainFeature>> GetForMap(Guid mapId)
    {
        if (_maps.GetById(mapId) is null)
        {
            return NotFound($"Map '{mapId}' does not exist.");
        }

        return Ok(_terrain.GetAll().Where(t => t.MapId == mapId));
    }

    [HttpPost("api/maps/{mapId:guid}/terrain")]
    public ActionResult<TerrainFeature> Create(Guid mapId, TerrainFeature terrain)
    {
        if (_maps.GetById(mapId) is null)
        {
            return BadRequest($"Map '{mapId}' does not exist.");
        }

        terrain.MapId = mapId;
        var created = _terrain.Create(terrain);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("api/terrain/{id:guid}")]
    public ActionResult<TerrainFeature> GetById(Guid id)
    {
        var terrain = _terrain.GetById(id);
        return terrain is null ? NotFound() : Ok(terrain);
    }

    [HttpPut("api/terrain/{id:guid}")]
    public IActionResult Update(Guid id, TerrainFeature terrain)
    {
        if (_maps.GetById(terrain.MapId) is null)
        {
            return BadRequest($"Map '{terrain.MapId}' does not exist.");
        }

        return _terrain.Update(id, terrain) ? NoContent() : NotFound();
    }

    [HttpDelete("api/terrain/{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        return _terrain.Delete(id) ? NoContent() : NotFound();
    }
}
