using FinanceApp.Application.Abstractions;
using FinanceApp.Contracts.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/accounts")]
public sealed class AccountsController(IAccountService accountService, ICurrentUserContext currentUser, IAuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await accountService.ListAsync(currentUser.UserId, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await accountService.GetAsync(currentUser.UserId, id, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAccountRequest request, CancellationToken cancellationToken)
    {
        var account = await accountService.CreateAsync(currentUser.UserId, request, cancellationToken);
        await auditService.WriteAsync(currentUser.UserId, "account.create", "account", account.Id, "success", "info", new { account.Name, account.AccountType }, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = account.Id }, account);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAccountRequest request, CancellationToken cancellationToken)
    {
        var result = await accountService.UpdateAsync(currentUser.UserId, id, request, cancellationToken);
        await auditService.WriteAsync(currentUser.UserId, "account.update", "account", id, "success", "info", new { request.Name, request.IsActive }, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await accountService.DeleteAsync(currentUser.UserId, id, cancellationToken);
        await auditService.WriteAsync(currentUser.UserId, "account.delete", "account", id, "success", "info", new { }, cancellationToken);
        return NoContent();
    }
}
