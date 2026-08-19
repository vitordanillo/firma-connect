using Firma.Connect.Api.Contracts;
using Firma.Connect.Api.Data;
using Firma.Connect.Api.Domain;
using Firma.Connect.Api.Features.Institutions;
using Microsoft.EntityFrameworkCore;

namespace Firma.Connect.Api.Features.Profiles;

public sealed record ProfileOperationResult(OwnProfileResponse? Profile, string? Error)
{
    public bool Succeeded => Profile is not null;
    public static ProfileOperationResult Success(OwnProfileResponse profile) => new(profile, null);
    public static ProfileOperationResult Failure(string error) => new(null, error);
}

public sealed class ProfileManagementService(FirmaDbContext db, TimeProvider timeProvider)
{
    public async Task<OwnProfileResponse?> GetAsync(Guid communityId, Guid userId, CancellationToken cancellationToken)
    {
        var profile = await LoadProfile(communityId, userId).SingleOrDefaultAsync(cancellationToken);
        return profile is null ? null : await MapAsync(profile, cancellationToken);
    }

    public async Task<ProfileOperationResult> UpsertAsync(
        Guid communityId,
        Guid userId,
        UpsertProfileRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = await ValidateAsync(request, cancellationToken);
        if (validationError is not null)
            return ProfileOperationResult.Failure(validationError);

        var profile = await LoadProfile(communityId, userId).SingleOrDefaultAsync(cancellationToken);
        if (profile is null)
        {
            profile = new Profile { CommunityId = communityId, UserId = userId };
            db.Profiles.Add(profile);
        }

        profile.InstitutionId = request.InstitutionId;
        profile.Course = Clean(request.Course);
        profile.Headline = Clean(request.Headline);
        profile.Bio = Clean(request.Bio);
        profile.ProjectName = Clean(request.ProjectName);
        profile.ProjectSummary = Clean(request.ProjectSummary);
        profile.CanHelpWith = Clean(request.CanHelpWith);
        profile.LookingFor = Clean(request.LookingFor);
        profile.ContactUrl = Clean(request.ContactUrl);
        profile.SetTeamSituation(request.TeamSituation);
        profile.VisibleInDirectory = request.VisibleInDirectory;
        profile.UpdatedAt = timeProvider.GetUtcNow();

        profile.Skills.Clear();
        foreach (var skill in await ResolveSkillsAsync(request.Skills ?? Array.Empty<string>(), cancellationToken))
            profile.Skills.Add(new ProfileSkill { Profile = profile, Skill = skill });

        profile.Interests.Clear();
        foreach (var interest in await ResolveInterestsAsync(request.Interests ?? Array.Empty<string>(), cancellationToken))
            profile.Interests.Add(new ProfileInterest { Profile = profile, Interest = interest });

        await db.SaveChangesAsync(cancellationToken);
        return ProfileOperationResult.Success(await MapAsync(profile, cancellationToken));
    }

    public async Task<bool> DeleteAsync(Guid communityId, Guid userId, CancellationToken cancellationToken)
    {
        var profile = await db.Profiles.SingleOrDefaultAsync(
            item => item.CommunityId == communityId && item.UserId == userId,
            cancellationToken);
        if (profile is null)
            return false;

        db.Profiles.Remove(profile);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<string?> ValidateAsync(UpsertProfileRequest request, CancellationToken cancellationToken)
    {
        if (request.Course?.Trim().Length > 120) return "O curso deve ter no máximo 120 caracteres.";
        if (request.Headline?.Trim().Length > 140) return "O título deve ter no máximo 140 caracteres.";
        if (request.Bio?.Trim().Length > 800) return "A apresentação deve ter no máximo 800 caracteres.";
        if (request.ProjectName?.Trim().Length > 120) return "O projeto deve ter no máximo 120 caracteres.";
        if (request.ProjectSummary?.Trim().Length > 500) return "O resumo do projeto deve ter no máximo 500 caracteres.";
        if (request.CanHelpWith?.Trim().Length > 300) return "O campo de ajuda deve ter no máximo 300 caracteres.";
        if (request.LookingFor?.Trim().Length > 300) return "O campo de busca deve ter no máximo 300 caracteres.";
        if (request.ContactUrl?.Trim().Length > 300) return "O contato deve ter no máximo 300 caracteres.";
        var skills = request.Skills ?? Array.Empty<string>();
        var interests = request.Interests ?? Array.Empty<string>();
        if (skills.Count > 10 || interests.Count > 10) return "Informe no máximo 10 competências e 10 interesses.";
        if (skills.Any(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length is < 2 or > 60)
            || interests.Any(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length is < 2 or > 60))
            return "Competências e interesses devem ter entre 2 e 60 caracteres.";
        if (!string.IsNullOrWhiteSpace(request.ContactUrl)
            && (!Uri.TryCreate(request.ContactUrl.Trim(), UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)))
            return "O contato deve ser uma URL HTTP ou HTTPS válida.";
        if (request.InstitutionId is not null
            && !await db.Institutions.AnyAsync(item => item.Id == request.InstitutionId, cancellationToken))
            return "Instituição não encontrada.";
        return null;
    }

    private async Task<IReadOnlyCollection<Skill>> ResolveSkillsAsync(
        IReadOnlyCollection<string> values,
        CancellationToken cancellationToken)
    {
        var normalized = NormalizeTags(values);
        var existing = await db.Skills.Where(item => normalized.Contains(item.NormalizedName)).ToListAsync(cancellationToken);
        foreach (var item in normalized.Where(value => existing.All(skill => skill.NormalizedName != value)))
            existing.Add(new Skill { Name = DisplayName(values, item), NormalizedName = item });
        return existing;
    }

    private async Task<IReadOnlyCollection<Interest>> ResolveInterestsAsync(
        IReadOnlyCollection<string> values,
        CancellationToken cancellationToken)
    {
        var normalized = NormalizeTags(values);
        var existing = await db.Interests.Where(item => normalized.Contains(item.NormalizedName)).ToListAsync(cancellationToken);
        foreach (var item in normalized.Where(value => existing.All(interest => interest.NormalizedName != value)))
            existing.Add(new Interest { Name = DisplayName(values, item), NormalizedName = item });
        return existing;
    }

    private async Task<OwnProfileResponse> MapAsync(Profile profile, CancellationToken cancellationToken)
    {
        var institution = profile.InstitutionId is null
            ? null
            : await db.Institutions.Where(item => item.Id == profile.InstitutionId).Select(item => item.Name).SingleAsync(cancellationToken);
        return new OwnProfileResponse(
            profile.Id, profile.CommunityId, profile.InstitutionId, institution, profile.Course, profile.Headline,
            profile.Bio, profile.ProjectName, profile.ProjectSummary, profile.CanHelpWith, profile.LookingFor,
            profile.ContactUrl, profile.TeamSituation,
            profile.Skills.Select(item => item.Skill.Name).Order().ToArray(),
            profile.Interests.Select(item => item.Interest.Name).Order().ToArray(),
            profile.VisibleInDirectory, profile.UpdatedAt);
    }

    private IQueryable<Profile> LoadProfile(Guid communityId, Guid userId)
        => db.Profiles
            .Include(profile => profile.Skills).ThenInclude(item => item.Skill)
            .Include(profile => profile.Interests).ThenInclude(item => item.Interest)
            .Where(profile => profile.CommunityId == communityId && profile.UserId == userId);

    private static string[] NormalizeTags(IEnumerable<string> values)
        => values.Select(InstitutionDirectoryService.Normalize).Distinct().ToArray();

    private static string DisplayName(IEnumerable<string> values, string normalized)
        => values.First(value => InstitutionDirectoryService.Normalize(value) == normalized).Trim();

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
