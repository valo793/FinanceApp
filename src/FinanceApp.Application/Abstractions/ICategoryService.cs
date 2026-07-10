using FinanceApp.Contracts.Categories;

namespace FinanceApp.Application.Abstractions;

public interface ICategoryService
{
    Task<IReadOnlyCollection<CategoryDto>> ListIncomeCategoriesAsync(Guid userId, CancellationToken cancellationToken);
    Task<CategoryDto> CreateIncomeCategoryAsync(Guid userId, CreateCategoryRequest request, CancellationToken cancellationToken);
    Task<CategoryDto> UpdateIncomeCategoryAsync(Guid userId, Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<CategoryDto>> ListExpenseCategoriesAsync(Guid userId, CancellationToken cancellationToken);
    Task<CategoryDto> CreateExpenseCategoryAsync(Guid userId, CreateCategoryRequest request, CancellationToken cancellationToken);
    Task<CategoryDto> UpdateExpenseCategoryAsync(Guid userId, Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken);
    Task DeleteIncomeCategoryAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task DeleteExpenseCategoryAsync(Guid userId, Guid id, CancellationToken cancellationToken);
}
