using Microsoft.AspNetCore.Mvc;
using WarmachineAPI.Models;
using WarmachineAPI.Services;

namespace WarmachineAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FactionsController : ControllerBase
{
    private readonly IRepository<Faction> _factions;

    public FactionsController(IRepository<Faction> factions)
    {
        _factions = factions;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Faction>> GetAll()
    {
        return Ok(_factions.GetAll());
    }

    [HttpGet("{id:guid}")]
    public ActionResult<Faction> GetById(Guid id)
    {
        var faction = _factions.GetById(id);
        return faction is null ? NotFound() : Ok(faction);
    }

    [HttpPost]
    public ActionResult<Faction> Create(Faction faction)
    {
        var created = _factions.Create(faction);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, Faction faction)
    {
        return _factions.Update(id, faction) ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        return _factions.Delete(id) ? NoContent() : NotFound();
    }
}
