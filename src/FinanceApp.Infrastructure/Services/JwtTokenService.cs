using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FinanceApp.Application.Abstractions;
using FinanceApp.Contracts.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FinanceApp.Infrastructure.Services;

public sealed class JwtTokenService(IConfiguration configuration) : ITokenService
{
    public LoginResponse IssueTokens(Guid userId, string email, string fullName, string theme)
    {
        var key = configuration["Jwt:SigningKey"] ?? "CHANGE_ME_SUPER_SECRET_32_CHARS_MINIMUM";
        var issuer = configuration["Jwt:Issuer"] ?? "FinanceApp";
        var audience = configuration["Jwt:Audience"] ?? "FinanceApp.Desktop";
        var expiresIn = int.TryParse(configuration["Jwt:AccessTokenSeconds"], out var seconds) ? seconds : 900;

        var tokenHandler = new JwtSecurityTokenHandler();
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Expires = DateTime.UtcNow.AddSeconds(expiresIn),
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim("name", fullName)
            ]),
            SigningCredentials = credentials
        };

        var token = tokenHandler.CreateToken(descriptor);

        return new LoginResponse
        {
            AccessToken = tokenHandler.WriteToken(token),
            ExpiresIn = expiresIn,
            RefreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            RequiresMfa = false,
            User = new CurrentUserDto
            {
                Id = userId,
                Email = email,
                FullName = fullName,
                Theme = theme
            }
        };
    }
}
