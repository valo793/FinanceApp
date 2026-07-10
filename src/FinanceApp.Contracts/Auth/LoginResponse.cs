namespace FinanceApp.Contracts.Auth;

public sealed class LoginResponse
{
    public required string AccessToken { get; init; }
    public required int ExpiresIn { get; init; }
    public required string RefreshToken { get; init; }
    public bool RequiresMfa { get; init; }
    public required CurrentUserDto User { get; init; }
}

public sealed class CurrentUserDto
{
    public required Guid Id { get; init; }
    public required string FullName { get; init; }
    public required string Theme { get; init; }
    public required string Email { get; init; }
}
