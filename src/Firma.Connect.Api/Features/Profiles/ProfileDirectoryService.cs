using Firma.Connect.Api.Contracts;
using Firma.Connect.Api.Data;
using Firma.Connect.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Firma.Connect.Api.Features.Profiles;

public sealed class ProfileDirectoryService(FirmaDbContext db)
{
    private const int MaxPageSize = 50;

    public async Task<ProfileDirectoryResponse> SearchAsync(
        Guid communityId,
        ProfileSearchQuery query,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        IQueryable<Profile> profiles = db.Profiles
            .AsNoTracking()
            .Where(profile => profile.CommunityId == communityId && profile.VisibleInDirectory);

        if (query.InstitutionId is not null)
            profiles = profiles.Where(profile => profile.InstitutionId == query.InstitutionId);

        if (query.AvailableForTeam is not null)
            profiles = profiles.Where(profile => profile.AvailableForTeam == query.AvailableForTeam);

        if (!string.IsNullOrWhiteSpace(query.Query))
        {
            var term = query.Query.Trim().ToLower();
            profiles = profiles.Where(profile =>
                (profile.Course != null && profile.Course.ToLower().Contains(term)) ||
                (profile.Headline != null && profile.Headline.ToLower().Contains(term)) ||
                db.Users.Any(user => user.Id == profile.UserId && user.DisplayName.ToLower().Contains(term)));
        }

        var total = await profiles.CountAsync(cancellationToken);
        var items = await profiles
            .OrderByDescending(profile => profile.AvailableForTeam)
            .ThenBy(profile => profile.UserId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Join(db.Users.AsNoTracking(), profile => profile.UserId, user => user.Id, (profile, user) => new { profile, user })
            .GroupJoin(db.Institutions.AsNoTracking(), value => value.profile.InstitutionId, institution => institution.Id,
                (value, institutions) => new { value.profile, value.user, institutions })
            .SelectMany(value => value.institutions.DefaultIfEmpty(), (value, institution) => new ProfileDirectoryItem(
                value.profile.Id,
                value.user.DisplayName,
                institution == null ? null : institution.Name,
                value.profile.Course,
                value.profile.Headline,
                value.profile.AvailableForTeam))
            .ToListAsync(cancellationToken);

        return new ProfileDirectoryResponse(items, total);
    }
}
