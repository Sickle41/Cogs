using Microsoft.AspNetCore.Mvc;
using WarmachineAPI.Models;
using WarmachineAPI.Services;

namespace WarmachineAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ArmiesController : ControllerBase
{
    private readonly IRepository<Army> _armies;
    private readonly IRepository<Faction> _factions;

    public ArmiesController(IRepository<Army> armies, IRepository<Faction> factions)
    {
        _armies = armies;
        _factions = factions;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Army>> GetAll()
    {
        return Ok(_armies.GetAll());
    }

    [HttpGet("{id:guid}")]
    public ActionResult<Army> GetById(Guid id)
    {
        var army = _armies.GetById(id);
        return army is null ? NotFound() : Ok(army);
    }

    [HttpPost]
    public ActionResult<Army> Create(Army army)
    {
        if (_factions.GetById(army.FactionId) is null)
        {
            return BadRequest($"Faction '{army.FactionId}' does not exist.");
        }

        var created = _armies.Create(army);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, Army army)
    {
        if (_factions.GetById(army.FactionId) is null)
        {
            return BadRequest($"Faction '{army.FactionId}' does not exist.");
        }

        return _armies.Update(id, army) ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        return _armies.Delete(id) ? NoContent() : NotFound();
    }
}
