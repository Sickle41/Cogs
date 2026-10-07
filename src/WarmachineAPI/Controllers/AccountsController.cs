using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
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
        account.ApiKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var created = _accounts.Create(account);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, Account account)
    {
        if (!IsOwnAccount(id))
        {
            return Forbid(ApiKeyAuthenticationHandler.SchemeName);
        }

        var existing = _accounts.GetById(id);
        if (existing is null)
        {
            return NotFound();
        }

        account.ApiKey = existing.ApiKey;
        return _accounts.Update(id, account) ? NoContent() : NotFound();
    }

    [Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        if (!IsOwnAccount(id))
        {
            return Forbid(ApiKeyAuthenticationHandler.SchemeName);
        }

        return _accounts.Delete(id) ? NoContent() : NotFound();
    }

    private bool IsOwnAccount(Guid id)
    {
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(callerId, out var parsed) && parsed == id;
    }
}
