namespace Firma.Connect.Api.Contracts;

public sealed record ProfileDirectoryItem(
    Guid Id,
    string DisplayName,
    string? Institution,
    string? Course,
    string? Headline,
    bool AvailableForTeam);

public sealed record ProfileDirectoryResponse(
    IReadOnlyCollection<ProfileDirectoryItem> Items,
    int Total);

public sealed record ProfileSearchQuery(
    Guid? InstitutionId,
    bool? AvailableForTeam,
    string? Query,
    int Page = 1,
    int PageSize = 20)
{
    public int Skip => (Page - 1) * PageSize;
}
