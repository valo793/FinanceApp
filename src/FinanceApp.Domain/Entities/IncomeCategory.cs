using FinanceApp.Domain.Common;

namespace FinanceApp.Domain.Entities;

public sealed class IncomeCategory : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Guid? ParentCategoryId { get; private set; }
    public string? Color { get; private set; }
    public string? Icon { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsSystem { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; } = true;

    private IncomeCategory() { }

    public IncomeCategory(Guid userId, string name, string? color = null, string? icon = null, bool isSystem = false)
    {
        UserId = userId;
        Name = name;
        Color = color;
        Icon = icon;
        IsSystem = isSystem;
    }

    public void Update(string name, string? color, string? icon, int displayOrder, bool isActive)
    {
        Name = name;
        Color = color;
        Icon = icon;
        DisplayOrder = displayOrder;
        IsActive = isActive;
        Touch();
    }
}
