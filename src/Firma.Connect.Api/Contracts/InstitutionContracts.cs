namespace Firma.Connect.Api.Contracts;

public sealed record InstitutionItem(Guid Id, string Name);

public sealed record InstitutionSearchResponse(
    IReadOnlyCollection<InstitutionItem> Items,
    int Total,
    int Page,
    int PageSize);

public sealed record InstitutionSearchQuery(string? Query, int Page = 1, int PageSize = 20);
