using Asp.Versioning;
using FinanceApp.Application.Abstractions;
using FinanceApp.Contracts.Categories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.Api.Controllers;

[ApiController]
[ApiVersion("1")]
[Route("api/v{v:apiVersion}/categories")]
[Authorize]
public sealed class CategoriesController(ICategoryService categoryService, ICurrentUserContext currentUser) : ControllerBase
{
    // ── Income Categories ───────────────────────────────────────
    [HttpGet("income")]
    public async Task<IActionResult> ListIncomeCategories(CancellationToken cancellationToken)
    {
        var result = await categoryService.ListIncomeCategoriesAsync(currentUser.UserId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("income")]
    public async Task<IActionResult> CreateIncomeCategory([FromBody] CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await categoryService.CreateIncomeCategoryAsync(currentUser.UserId, request, cancellationToken);
        return Created($"api/v1/categories/income/{result.Id}", result);
    }

    [HttpPut("income/{id:guid}")]
    public async Task<IActionResult> UpdateIncomeCategory(Guid id, [FromBody] UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await categoryService.UpdateIncomeCategoryAsync(currentUser.UserId, id, request, cancellationToken);
        return Ok(result);
    }

    // ── Expense Categories ──────────────────────────────────────
    [HttpGet("expense")]
    public async Task<IActionResult> ListExpenseCategories(CancellationToken cancellationToken)
    {
        var result = await categoryService.ListExpenseCategoriesAsync(currentUser.UserId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("expense")]
    public async Task<IActionResult> CreateExpenseCategory([FromBody] CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await categoryService.CreateExpenseCategoryAsync(currentUser.UserId, request, cancellationToken);
        return Created($"api/v1/categories/expense/{result.Id}", result);
    }

    [HttpPut("expense/{id:guid}")]
    public async Task<IActionResult> UpdateExpenseCategory(Guid id, [FromBody] UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await categoryService.UpdateExpenseCategoryAsync(currentUser.UserId, id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("income/{id:guid}")]
    public async Task<IActionResult> DeleteIncomeCategory(Guid id, CancellationToken cancellationToken)
    {
        await categoryService.DeleteIncomeCategoryAsync(currentUser.UserId, id, cancellationToken);
        return NoContent();
    }

    [HttpDelete("expense/{id:guid}")]
    public async Task<IActionResult> DeleteExpenseCategory(Guid id, CancellationToken cancellationToken)
    {
        await categoryService.DeleteExpenseCategoryAsync(currentUser.UserId, id, cancellationToken);
        return NoContent();
    }
}
