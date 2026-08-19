using Firma.Connect.Api.Contracts;
using Firma.Connect.Api.Data;
using Firma.Connect.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Firma.Connect.Api.Features.Teams;

public sealed class TeamDiscoveryService(FirmaDbContext db)
{
    public async Task<TeamDiscoverySummary?> GetSummaryAsync(
        Guid communityId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var profile = await db.Profiles.AsNoTracking().SingleOrDefaultAsync(
            item => item.CommunityId == communityId && item.UserId == userId,
            cancellationToken);
        if (profile?.InstitutionId is null) return null;

        var institutionId = profile.InstitutionId.Value;
        var institution = await db.Institutions.AsNoTracking()
            .Where(item => item.Id == institutionId)
            .Select(item => item.Name)
            .SingleAsync(cancellationToken);
        var institutionProfiles = db.Profiles.AsNoTracking().Where(item =>
            item.CommunityId == communityId && item.InstitutionId == institutionId && item.VisibleInDirectory);

        return new TeamDiscoverySummary(
            institutionId,
            institution,
            await institutionProfiles.CountAsync(cancellationToken),
            await institutionProfiles.CountAsync(item => item.TeamSituation == TeamSituation.LookingForTeam, cancellationToken),
            await db.Teams.AsNoTracking().CountAsync(item => item.CommunityId == communityId
                && item.InstitutionId == institutionId && item.IsOpen && item.Members.Count < Team.MaximumMembers, cancellationToken),
            await institutionProfiles.CountAsync(item => item.TeamSituation == TeamSituation.HasTeam, cancellationToken));
    }
}
