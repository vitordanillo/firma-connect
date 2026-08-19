namespace Firma.Connect.Api.Contracts;

using Firma.Connect.Api.Domain;

public sealed record UpsertProfileRequest(
    Guid? InstitutionId,
    string? Course,
    string? Headline,
    string? Bio,
    string? ProjectName,
    string? ProjectSummary,
    string? CanHelpWith,
    string? LookingFor,
    string? ContactUrl,
    TeamSituation TeamSituation,
    IReadOnlyCollection<string> Skills,
    IReadOnlyCollection<string> Interests,
    bool VisibleInDirectory);

public sealed record OwnProfileResponse(
    Guid Id,
    Guid CommunityId,
    Guid? InstitutionId,
    string? Institution,
    string? Course,
    string? Headline,
    string? Bio,
    string? ProjectName,
    string? ProjectSummary,
    string? CanHelpWith,
    string? LookingFor,
    string? ContactUrl,
    TeamSituation TeamSituation,
    IReadOnlyCollection<string> Skills,
    IReadOnlyCollection<string> Interests,
    bool VisibleInDirectory,
    DateTimeOffset UpdatedAt);
