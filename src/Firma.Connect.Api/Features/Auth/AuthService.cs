using Firma.Connect.Api.Contracts;
using Firma.Connect.Api.Data;
using Firma.Connect.Api.Domain;
using Firma.Connect.Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Firma.Connect.Api.Features.Auth;

public sealed record AuthResult(AuthResponse? Response, string? Error)
{
    public bool Succeeded => Response is not null;
    public static AuthResult Success(AuthResponse response) => new(response, null);
    public static AuthResult Failure(string error) => new(null, error);
}

public sealed class AuthService(
    FirmaDbContext db,
    IPasswordHasher<User> passwordHasher,
    JwtTokenService tokenService,
    TimeProvider timeProvider)
{
    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.InvitationToken))
            return AuthResult.Failure("Convite inválido ou expirado.");
        if (string.IsNullOrWhiteSpace(request.DisplayName) || request.DisplayName.Trim().Length > 100)
            return AuthResult.Failure("Nome inválido.");
        if (string.IsNullOrEmpty(request.Password) || request.Password.Length < 10)
            return AuthResult.Failure("A senha deve ter pelo menos 10 caracteres.");

        var tokenHash = TokenGenerator.Hash(request.InvitationToken.Trim());
        var invitation = await db.CommunityInvitations
            .SingleOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);
        if (invitation is null || !invitation.CanBeUsedAt(timeProvider.GetUtcNow()))
            return AuthResult.Failure("Convite inválido ou expirado.");

        var normalizedEmail = invitation.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(user => user.Email == normalizedEmail, cancellationToken))
            return AuthResult.Failure("Já existe uma conta com este e-mail.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var user = new User { Email = normalizedEmail, DisplayName = request.DisplayName.Trim() };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        db.CommunityMemberships.Add(new CommunityMembership
        {
            CommunityId = invitation.CommunityId,
            UserId = user.Id
        });
        invitation.UsedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return AuthResult.Success(tokenService.Create(user));
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password))
            return AuthResult.Failure("E-mail ou senha inválidos.");

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(
            item => item.Email == normalizedEmail && item.DeletedAt == null,
            cancellationToken);
        if (user is null)
            return AuthResult.Failure("E-mail ou senha inválidos.");

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
            return AuthResult.Failure("E-mail ou senha inválidos.");
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
            await db.SaveChangesAsync(cancellationToken);
        }

        return AuthResult.Success(tokenService.Create(user));
    }
}
