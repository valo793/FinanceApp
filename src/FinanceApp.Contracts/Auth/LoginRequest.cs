namespace FinanceApp.Contracts.Auth;

public sealed class LoginRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public string? DeviceName { get; init; }
    public bool TrustDevice { get; init; }
}
