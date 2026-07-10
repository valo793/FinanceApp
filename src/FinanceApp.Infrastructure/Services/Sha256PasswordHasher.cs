using System.Security.Cryptography;
using System.Text;
using FinanceApp.Application.Abstractions;

namespace FinanceApp.Infrastructure.Services;

// Placeholder seguro apenas para scaffold.
// Substituir por Argon2id ou ASP.NET Core Identity antes de produção.
public sealed class Sha256PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    public bool Verify(string hash, string password) => Hash(password) == hash;
}
