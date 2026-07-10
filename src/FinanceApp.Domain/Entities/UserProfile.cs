namespace FinanceApp.Domain.Entities;

public sealed class UserProfile
{
    public Guid UserId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string PreferredCurrency { get; private set; } = "BRL";
    public string Timezone { get; private set; } = "America/Sao_Paulo";
    public int FinancialMonthStartDay { get; private set; } = 1;
    public string Locale { get; private set; } = "pt-BR";
    public string? AvatarUrl { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private UserProfile() { }

    public UserProfile(Guid userId, string fullName)
    {
        UserId = userId;
        FullName = fullName;
    }
}
