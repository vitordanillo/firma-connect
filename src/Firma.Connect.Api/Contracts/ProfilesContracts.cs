namespace Firma.Connect.Api.Contracts;

using Firma.Connect.Api.Domain;

public sealed record ProfileDirectoryItem(
    Guid Id,
    string DisplayName,
    string? Institution,
    string? Course,
    string? Headline,
    string? ProjectName,
    string? CanHelpWith,
    string? LookingFor,
    TeamSituation TeamSituation,
    IReadOnlyCollection<string> Skills,
    IReadOnlyCollection<string> Interests);

public sealed record ProfileDirectoryResponse(
    IReadOnlyCollection<ProfileDirectoryItem> Items,
    int Total);

public sealed record ProfileSearchQuery(
    Guid? InstitutionId,
    bool? AvailableForTeam,
    string? Query,
    string? Skill = null,
    string? Interest = null,
    TeamSituation? TeamSituation = null,
    bool SameInstitutionFirst = true,
    int Page = 1,
    int PageSize = 20)
{
    public int Skip => (Page - 1) * PageSize;
}
