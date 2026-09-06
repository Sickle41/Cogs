using Microsoft.AspNetCore.Mvc;
using WarmachineAPI.Models;
using WarmachineAPI.Services;

namespace WarmachineAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MapsController : ControllerBase
{
    private readonly IRepository<Map> _maps;

    public MapsController(IRepository<Map> maps)
    {
        _maps = maps;
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
        return _maps.Delete(id) ? NoContent() : NotFound();
    }
}
