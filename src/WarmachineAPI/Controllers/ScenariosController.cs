using Microsoft.AspNetCore.Mvc;
using WarmachineAPI.Models;
using WarmachineAPI.Services;

namespace WarmachineAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScenariosController : ControllerBase
{
    private readonly IRepository<Scenario> _scenarios;

    public ScenariosController(IRepository<Scenario> scenarios)
    {
        _scenarios = scenarios;
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
        return _scenarios.Delete(id) ? NoContent() : NotFound();
    }
}
