using Microsoft.AspNetCore.Mvc;
using WarmachineAPI.Models;
using WarmachineAPI.Services;

namespace WarmachineAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IRepository<Account> _accounts;

    public AccountsController(IRepository<Account> accounts)
    {
        _accounts = accounts;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Account>> GetAll()
    {
        return Ok(_accounts.GetAll());
    }

    [HttpGet("{id:guid}")]
    public ActionResult<Account> GetById(Guid id)
    {
        var account = _accounts.GetById(id);
        return account is null ? NotFound() : Ok(account);
    }

    [HttpPost]
    public ActionResult<Account> Create(Account account)
    {
        var created = _accounts.Create(account);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, Account account)
    {
        return _accounts.Update(id, account) ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        return _accounts.Delete(id) ? NoContent() : NotFound();
    }
}
