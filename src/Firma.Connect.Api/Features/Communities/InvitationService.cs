using System.ComponentModel.DataAnnotations;
using Firma.Connect.Api.Contracts;
using Firma.Connect.Api.Data;
using Firma.Connect.Api.Domain;
using Firma.Connect.Api.Security;

namespace Firma.Connect.Api.Features.Communities;

public sealed class InvitationService(FirmaDbContext db, TimeProvider timeProvider)
{
    public async Task<InvitationResponse?> CreateAsync(
        Guid communityId,
        Guid createdByUserId,
        CreateInvitationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return null;

        var email = request.Email.Trim().ToLowerInvariant();
        if (!new EmailAddressAttribute().IsValid(email))
            return null;

        var token = TokenGenerator.CreateSecureToken();
        var invitation = new CommunityInvitation
        {
            CommunityId = communityId,
            Email = email,
            TokenHash = TokenGenerator.Hash(token),
            CreatedByUserId = createdByUserId,
            ExpiresAt = timeProvider.GetUtcNow().AddDays(3)
        };
        db.CommunityInvitations.Add(invitation);
        await db.SaveChangesAsync(cancellationToken);

        return new InvitationResponse(invitation.Id, token, invitation.ExpiresAt);
    }
}
