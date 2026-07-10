using FinanceApp.Application.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace FinanceApp.Infrastructure.Services;

/// <summary>
/// Secure password hasher using ASP.NET Core Identity's PBKDF2 implementation
/// with proper salting and iterative stretching.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private static readonly PasswordHasher<string> _hasher = new();

    public string Hash(string password)
    {
        return _hasher.HashPassword("financeapp", password);
    }

    public bool Verify(string hash, string password)
    {
        var result = _hasher.VerifyHashedPassword("financeapp", hash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
