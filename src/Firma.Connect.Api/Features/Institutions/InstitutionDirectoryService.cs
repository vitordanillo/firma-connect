using System.Globalization;
using System.Text;
using Firma.Connect.Api.Contracts;
using Firma.Connect.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Firma.Connect.Api.Features.Institutions;

public sealed class InstitutionDirectoryService(FirmaDbContext db)
{
    private const int MaxPageSize = 50;

    public async Task<InstitutionSearchResponse> SearchAsync(
        InstitutionSearchQuery query,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var institutions = db.Institutions.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Query))
        {
            var term = Normalize(query.Query);
            institutions = institutions.Where(institution => institution.NormalizedName.Contains(term));
        }

        var total = await institutions.CountAsync(cancellationToken);
        var items = await institutions
            .OrderBy(institution => institution.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(institution => new InstitutionItem(institution.Id, institution.Name))
            .ToListAsync(cancellationToken);

        return new InstitutionSearchResponse(items, total, page, pageSize);
    }

    internal static string Normalize(string value)
    {
        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
