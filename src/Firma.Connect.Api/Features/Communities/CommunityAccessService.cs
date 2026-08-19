using Firma.Connect.Api.Data;
using Firma.Connect.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Firma.Connect.Api.Features.Communities;

public sealed class CommunityAccessService(FirmaDbContext db)
{
    public Task<bool> IsMemberAsync(Guid communityId, Guid userId, CancellationToken cancellationToken)
        => db.CommunityMemberships.AsNoTracking().AnyAsync(
            membership => membership.CommunityId == communityId && membership.UserId == userId,
            cancellationToken);

    public Task<bool> IsAdminAsync(Guid communityId, Guid userId, CancellationToken cancellationToken)
        => db.CommunityMemberships.AsNoTracking().AnyAsync(
            membership => membership.CommunityId == communityId
                && membership.UserId == userId
                && membership.Role == MembershipRole.Admin,
            cancellationToken);
}
