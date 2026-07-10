using FinanceApp.Application.Abstractions;
using FinanceApp.Contracts.Auth;
using FinanceApp.Domain.Entities;
using FinanceApp.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace FinanceApp.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    FinanceDbContext dbContext,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IAuditService auditService,
    IValidator<LoginRequest> validator) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var exists = await dbContext.Users.AnyAsync(x => x.Email == request.Email.Trim().ToLower(), cancellationToken);
        if (exists)
            return Conflict(new ProblemDetails { Title = "E-mail já cadastrado.", Status = 409 });

        var user = new User(request.Email, passwordHasher.Hash(request.Password));
        var profile = new UserProfile(user.Id, request.Email.Split('@')[0]);
        var preference = new UserPreference(user.Id);
        dbContext.Users.Add(user);
        dbContext.UserProfiles.Add(profile);
        dbContext.UserPreferences.Add(preference);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(user.Id, "auth.register", "user", user.Id, "success", "info", new { request.Email }, cancellationToken);

        var tokens = tokenService.IssueTokens(user.Id, user.Email, profile.FullName, "dark");

        // Create session for refresh token
        var refreshTokenHash = HashToken(tokens.RefreshToken);
        var session = new Session(user.Id, refreshTokenHash, DateTimeOffset.UtcNow.AddDays(7), request.DeviceName);
        dbContext.Sessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(tokens);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

        // Check lockout before validating password
        if (user is not null && user.IsLockedOut)
        {
            await auditService.WriteAsync(user.Id, "auth.login.locked", "user", user.Id, "denied", "warning", new { email }, cancellationToken);
            return Unauthorized(new ProblemDetails { Title = "Conta bloqueada por excesso de tentativas. Tente novamente mais tarde.", Status = 401 });
        }

        if (user is null || !passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            if (user is not null)
            {
                user.RegisterLoginFailure();
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            await auditService.WriteAsync(user?.Id, "auth.login.failed", "user", user?.Id, "failed", "warning", new { email }, cancellationToken);
            return Unauthorized(new ProblemDetails { Title = "Credenciais inválidas.", Status = 401 });
        }

        user.RegisterLoginSuccess();
        var profile = await dbContext.UserProfiles.FirstAsync(x => x.UserId == user.Id, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(user.Id, "auth.login.success", "user", user.Id, "success", "info", new { email }, cancellationToken);

        var tokens = tokenService.IssueTokens(user.Id, user.Email, profile.FullName, "dark");

        // Create session
        var refreshTokenHash = HashToken(tokens.RefreshToken);
        var session = new Session(user.Id, refreshTokenHash, DateTimeOffset.UtcNow.AddDays(7), request.DeviceName);
        dbContext.Sessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(tokens);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var hash = HashToken(request.RefreshToken);
        var session = await dbContext.Sessions.FirstOrDefaultAsync(x => x.RefreshTokenHash == hash, cancellationToken);

        if (session is null || !session.IsValid)
            return Unauthorized(new ProblemDetails { Title = "Refresh token inválido ou expirado.", Status = 401 });

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == session.UserId, cancellationToken);
        if (user is null)
            return Unauthorized(new ProblemDetails { Title = "Usuário não encontrado.", Status = 401 });

        var profile = await dbContext.UserProfiles.FirstAsync(x => x.UserId == user.Id, cancellationToken);

        // Rotate: revoke current session and create a new one
        var newTokens = tokenService.IssueTokens(user.Id, user.Email, profile.FullName, "dark");
        var newHash = HashToken(newTokens.RefreshToken);
        var newSession = new Session(user.Id, newHash, DateTimeOffset.UtcNow.AddDays(7), session.DeviceName);
        session.MarkReplaced(newSession.Id);
        dbContext.Sessions.Add(newSession);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(newTokens);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        // Revoke all sessions for the current user
        var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
        var sessions = await dbContext.Sessions.Where(x => x.UserId == userId && x.RevokedAt == null).ToListAsync(cancellationToken);
        foreach (var s in sessions) s.Revoke();
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("sessions")]
    [Authorize]
    public async Task<IActionResult> Sessions(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
        var sessions = await dbContext.Sessions
            .Where(x => x.UserId == userId && x.RevokedAt == null && x.ExpiresAt > DateTimeOffset.UtcNow)
            .OrderByDescending(x => x.LastActivityAt)
            .Select(x => new { x.Id, x.DeviceName, x.LastActivityAt, x.CreatedAt })
            .ToListAsync(cancellationToken);
        return Ok(sessions);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}

public sealed class RefreshTokenRequest
{
    public required string RefreshToken { get; init; }
}
