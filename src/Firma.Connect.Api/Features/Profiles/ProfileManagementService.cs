using Firma.Connect.Api.Contracts;
using Firma.Connect.Api.Data;
using Firma.Connect.Api.Domain;
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
    public async Task<OwnProfileResponse?> GetAsync(
        Guid communityId,
        Guid userId,
        CancellationToken cancellationToken)
        => await QueryProfile(communityId, userId).SingleOrDefaultAsync(cancellationToken);

    public async Task<ProfileOperationResult> UpsertAsync(
        Guid communityId,
        Guid userId,
        UpsertProfileRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = await ValidateAsync(request, cancellationToken);
        if (validationError is not null)
            return ProfileOperationResult.Failure(validationError);

        var profile = await db.Profiles.SingleOrDefaultAsync(
            item => item.CommunityId == communityId && item.UserId == userId,
            cancellationToken);
        if (profile is null)
        {
            profile = new Profile { CommunityId = communityId, UserId = userId };
            db.Profiles.Add(profile);
        }

        profile.InstitutionId = request.InstitutionId;
        profile.Course = Clean(request.Course);
        profile.Headline = Clean(request.Headline);
        profile.Bio = Clean(request.Bio);
        profile.ContactUrl = Clean(request.ContactUrl);
        profile.AvailableForTeam = request.AvailableForTeam;
        profile.VisibleInDirectory = request.VisibleInDirectory;
        profile.UpdatedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);

        var response = await QueryProfile(communityId, userId).SingleAsync(cancellationToken);
        return ProfileOperationResult.Success(response);
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
        if (request.Course?.Trim().Length > 120)
            return "O curso deve ter no máximo 120 caracteres.";
        if (request.Headline?.Trim().Length > 140)
            return "O título deve ter no máximo 140 caracteres.";
        if (request.Bio?.Trim().Length > 800)
            return "A apresentação deve ter no máximo 800 caracteres.";
        if (request.ContactUrl?.Trim().Length > 300)
            return "O contato deve ter no máximo 300 caracteres.";
        if (!string.IsNullOrWhiteSpace(request.ContactUrl)
            && (!Uri.TryCreate(request.ContactUrl.Trim(), UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)))
            return "O contato deve ser uma URL HTTP ou HTTPS válida.";
        if (request.InstitutionId is not null
            && !await db.Institutions.AnyAsync(item => item.Id == request.InstitutionId, cancellationToken))
            return "Instituição não encontrada.";

        return null;
    }

    private IQueryable<OwnProfileResponse> QueryProfile(Guid communityId, Guid userId)
        => db.Profiles
            .AsNoTracking()
            .Where(profile => profile.CommunityId == communityId && profile.UserId == userId)
            .GroupJoin(db.Institutions.AsNoTracking(), profile => profile.InstitutionId, institution => institution.Id,
                (profile, institutions) => new { profile, institutions })
            .SelectMany(value => value.institutions.DefaultIfEmpty(), (value, institution) => new OwnProfileResponse(
                value.profile.Id,
                value.profile.CommunityId,
                value.profile.InstitutionId,
                institution == null ? null : institution.Name,
                value.profile.Course,
                value.profile.Headline,
                value.profile.Bio,
                value.profile.ContactUrl,
                value.profile.AvailableForTeam,
                value.profile.VisibleInDirectory,
                value.profile.UpdatedAt));

    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
