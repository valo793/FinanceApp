using FinanceApp.Application.Abstractions;
using FinanceApp.Contracts.Transactions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/transactions")]
public sealed class TransactionsController(ITransactionService transactionService, ICurrentUserContext currentUser, IAuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default) =>
        Ok(await transactionService.ListAsync(currentUser.UserId, page, Math.Clamp(pageSize, 1, 100), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await transactionService.GetAsync(currentUser.UserId, id, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertTransactionRequest request, CancellationToken cancellationToken)
    {
        var transaction = await transactionService.CreateAsync(currentUser.UserId, request, cancellationToken);
        await auditService.WriteAsync(currentUser.UserId, "transaction.create", "transaction", transaction.Id, "success", "info", new { request.TransactionType, request.Description }, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = transaction.Id }, transaction);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertTransactionRequest request, CancellationToken cancellationToken)
    {
        var result = await transactionService.UpdateAsync(currentUser.UserId, id, request, cancellationToken);
        await auditService.WriteAsync(currentUser.UserId, "transaction.update", "transaction", id, "success", "info", new { request.TransactionType, request.Description }, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await transactionService.DeleteAsync(currentUser.UserId, id, cancellationToken);
        await auditService.WriteAsync(currentUser.UserId, "transaction.delete", "transaction", id, "success", "info", new { }, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken cancellationToken)
    {
        await transactionService.ConfirmExpenseAsync(currentUser.UserId, id, cancellationToken);
        await auditService.WriteAsync(currentUser.UserId, "transaction.confirm", "transaction", id, "success", "info", new { }, cancellationToken);
        return NoContent();
    }
}
