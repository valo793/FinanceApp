namespace FinanceApp.Contracts.Categories;

public sealed class CategoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid? ParentCategoryId { get; init; }
    public string? Color { get; init; }
    public string? Icon { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsSystem { get; init; }
    public bool IsDefault { get; init; }
    public bool IsActive { get; init; }
    public long LockVersion { get; init; }
}

public sealed class CreateCategoryRequest
{
    public required string Name { get; init; }
    public string? Color { get; init; }
    public string? Icon { get; init; }
}

public sealed class UpdateCategoryRequest
{
    public required string Name { get; init; }
    public string? Color { get; init; }
    public string? Icon { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; } = true;
    public required long LockVersion { get; init; }
}
