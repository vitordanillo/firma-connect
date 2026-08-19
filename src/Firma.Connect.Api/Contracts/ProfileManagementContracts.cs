namespace Firma.Connect.Api.Contracts;

public sealed record UpsertProfileRequest(
    Guid? InstitutionId,
    string? Course,
    string? Headline,
    string? Bio,
    string? ContactUrl,
    bool AvailableForTeam,
    bool VisibleInDirectory);

public sealed record OwnProfileResponse(
    Guid Id,
    Guid CommunityId,
    Guid? InstitutionId,
    string? Institution,
    string? Course,
    string? Headline,
    string? Bio,
    string? ContactUrl,
    bool AvailableForTeam,
    bool VisibleInDirectory,
    DateTimeOffset UpdatedAt);
