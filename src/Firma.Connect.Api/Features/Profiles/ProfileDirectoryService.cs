using Firma.Connect.Api.Contracts;
using Firma.Connect.Api.Data;
using Firma.Connect.Api.Domain;
using Firma.Connect.Api.Features.Institutions;
using Microsoft.EntityFrameworkCore;

namespace Firma.Connect.Api.Features.Profiles;

public sealed class ProfileDirectoryService(FirmaDbContext db)
{
    private const int MaxPageSize = 50;

    public async Task<ProfileDirectoryResponse> SearchAsync(
        Guid communityId,
        Guid requesterUserId,
        ProfileSearchQuery query,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var requesterInstitutionId = await db.Profiles.AsNoTracking()
            .Where(profile => profile.CommunityId == communityId && profile.UserId == requesterUserId)
            .Select(profile => profile.InstitutionId)
            .SingleOrDefaultAsync(cancellationToken);

        IQueryable<Profile> profiles = db.Profiles.AsNoTracking()
            .Where(profile => profile.CommunityId == communityId && profile.VisibleInDirectory);

        if (query.InstitutionId is not null)
            profiles = profiles.Where(profile => profile.InstitutionId == query.InstitutionId);
        if (query.AvailableForTeam is not null)
            profiles = profiles.Where(profile => profile.AvailableForTeam == query.AvailableForTeam);
        if (query.TeamSituation is not null)
            profiles = profiles.Where(profile => profile.TeamSituation == query.TeamSituation);
        if (!string.IsNullOrWhiteSpace(query.Skill))
        {
            var skill = InstitutionDirectoryService.Normalize(query.Skill);
            profiles = profiles.Where(profile => profile.Skills.Any(item => item.Skill.NormalizedName == skill));
        }
        if (!string.IsNullOrWhiteSpace(query.Interest))
        {
            var interest = InstitutionDirectoryService.Normalize(query.Interest);
            profiles = profiles.Where(profile => profile.Interests.Any(item => item.Interest.NormalizedName == interest));
        }
        if (!string.IsNullOrWhiteSpace(query.Query))
        {
            var term = query.Query.Trim().ToLowerInvariant();
            profiles = profiles.Where(profile =>
                (profile.Course != null && profile.Course.ToLower().Contains(term))
                || (profile.Headline != null && profile.Headline.ToLower().Contains(term))
                || (profile.ProjectName != null && profile.ProjectName.ToLower().Contains(term))
                || (profile.CanHelpWith != null && profile.CanHelpWith.ToLower().Contains(term))
                || (profile.LookingFor != null && profile.LookingFor.ToLower().Contains(term))
                || db.Users.Any(user => user.Id == profile.UserId && user.DisplayName.ToLower().Contains(term)));
        }

        var total = await profiles.CountAsync(cancellationToken);
        var ordered = query.SameInstitutionFirst && requesterInstitutionId is not null
            ? profiles.OrderByDescending(profile => profile.InstitutionId == requesterInstitutionId)
                .ThenBy(profile => profile.TeamSituation != TeamSituation.LookingForTeam)
                .ThenBy(profile => profile.UserId)
            : profiles.OrderBy(profile => profile.TeamSituation != TeamSituation.LookingForTeam)
                .ThenBy(profile => profile.UserId);

        var baseItems = await ordered.Skip((page - 1) * pageSize).Take(pageSize)
            .Join(db.Users.AsNoTracking(), profile => profile.UserId, user => user.Id, (profile, user) => new { profile, user })
            .GroupJoin(db.Institutions.AsNoTracking(), value => value.profile.InstitutionId, institution => institution.Id,
                (value, institutions) => new { value.profile, value.user, institutions })
            .SelectMany(value => value.institutions.DefaultIfEmpty(), (value, institution) => new
            {
                value.profile.Id,
                value.user.DisplayName,
                Institution = institution == null ? null : institution.Name,
                value.profile.Course,
                value.profile.Headline,
                value.profile.ProjectName,
                value.profile.CanHelpWith,
                value.profile.LookingFor,
                value.profile.TeamSituation
            }).ToListAsync(cancellationToken);

        var profileIds = baseItems.Select(item => item.Id).ToArray();
        var skills = await db.ProfileSkills.AsNoTracking().Where(item => profileIds.Contains(item.ProfileId))
            .GroupBy(item => item.ProfileId)
            .ToDictionaryAsync(group => group.Key, group => group.Select(item => item.Skill.Name).Order().ToArray(), cancellationToken);
        var interests = await db.ProfileInterests.AsNoTracking().Where(item => profileIds.Contains(item.ProfileId))
            .GroupBy(item => item.ProfileId)
            .ToDictionaryAsync(group => group.Key, group => group.Select(item => item.Interest.Name).Order().ToArray(), cancellationToken);

        var items = baseItems.Select(item => new ProfileDirectoryItem(
            item.Id, item.DisplayName, item.Institution, item.Course, item.Headline, item.ProjectName,
            item.CanHelpWith, item.LookingFor, item.TeamSituation,
            skills.GetValueOrDefault(item.Id, []), interests.GetValueOrDefault(item.Id, []))).ToArray();
        return new ProfileDirectoryResponse(items, total);
    }
}
