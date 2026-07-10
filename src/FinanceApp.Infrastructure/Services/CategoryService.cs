using FinanceApp.Application.Abstractions;
using FinanceApp.Contracts.Categories;
using FinanceApp.Domain.Entities;
using FinanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Services;

public sealed class CategoryService(FinanceDbContext dbContext) : ICategoryService
{
    // ── Income Categories ───────────────────────────────────────
    public async Task<IReadOnlyCollection<CategoryDto>> ListIncomeCategoriesAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.IncomeCategories
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .Select(x => new CategoryDto
            {
                Id = x.Id, Name = x.Name, ParentCategoryId = x.ParentCategoryId,
                Color = x.Color, Icon = x.Icon, DisplayOrder = x.DisplayOrder,
                IsSystem = x.IsSystem, IsDefault = x.IsDefault, IsActive = x.IsActive,
                LockVersion = x.LockVersion
            })
            .ToListAsync(cancellationToken);

    public async Task<CategoryDto> CreateIncomeCategoryAsync(Guid userId, CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var entity = new IncomeCategory(userId, request.Name, request.Color, request.Icon);
        dbContext.IncomeCategories.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapIncome(entity);
    }

    public async Task<CategoryDto> UpdateIncomeCategoryAsync(Guid userId, Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.IncomeCategories.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Categoria não encontrada.");
        entity.Update(request.Name, request.Color, request.Icon, request.DisplayOrder, request.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapIncome(entity);
    }

    // ── Expense Categories ──────────────────────────────────────
    public async Task<IReadOnlyCollection<CategoryDto>> ListExpenseCategoriesAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.ExpenseCategories
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .Select(x => new CategoryDto
            {
                Id = x.Id, Name = x.Name, ParentCategoryId = x.ParentCategoryId,
                Color = x.Color, Icon = x.Icon, DisplayOrder = x.DisplayOrder,
                IsSystem = x.IsSystem, IsDefault = x.IsDefault, IsActive = x.IsActive,
                LockVersion = x.LockVersion
            })
            .ToListAsync(cancellationToken);

    public async Task<CategoryDto> CreateExpenseCategoryAsync(Guid userId, CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var entity = new ExpenseCategory(userId, request.Name, request.Color, request.Icon);
        dbContext.ExpenseCategories.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapExpense(entity);
    }

    public async Task<CategoryDto> UpdateExpenseCategoryAsync(Guid userId, Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ExpenseCategories.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Categoria não encontrada.");
        entity.Update(request.Name, request.Color, request.Icon, request.DisplayOrder, request.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapExpense(entity);
    }

    public async Task DeleteIncomeCategoryAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.IncomeCategories.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id, cancellationToken);
        if (entity != null)
        {
            dbContext.IncomeCategories.Remove(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteExpenseCategoryAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ExpenseCategories.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id, cancellationToken);
        if (entity != null)
        {
            dbContext.ExpenseCategories.Remove(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    // ── Mappers ─────────────────────────────────────────────────
    private static CategoryDto MapIncome(IncomeCategory x) => new()
    {
        Id = x.Id, Name = x.Name, ParentCategoryId = x.ParentCategoryId,
        Color = x.Color, Icon = x.Icon, DisplayOrder = x.DisplayOrder,
        IsSystem = x.IsSystem, IsDefault = x.IsDefault, IsActive = x.IsActive,
        LockVersion = x.LockVersion
    };

    private static CategoryDto MapExpense(ExpenseCategory x) => new()
    {
        Id = x.Id, Name = x.Name, ParentCategoryId = x.ParentCategoryId,
        Color = x.Color, Icon = x.Icon, DisplayOrder = x.DisplayOrder,
        IsSystem = x.IsSystem, IsDefault = x.IsDefault, IsActive = x.IsActive,
        LockVersion = x.LockVersion
    };
}
