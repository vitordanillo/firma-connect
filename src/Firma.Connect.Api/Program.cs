using Firma.Connect.Api.Data;
using Firma.Connect.Api.Contracts;
using Firma.Connect.Api.Features.Profiles;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<FirmaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"))
           .UseSnakeCaseNamingConvention());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks().AddDbContextCheck<FirmaDbContext>();
builder.Services.AddScoped<ProfileDirectoryService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapHealthChecks("/health");

// TODO: Protect this endpoint with community-membership authorization once identity is added.
app.MapGet("/api/communities/{communityId:guid}/profiles", async (
    Guid communityId,
    [AsParameters] ProfileSearchQuery query,
    ProfileDirectoryService directory,
    CancellationToken cancellationToken) =>
{
    var result = await directory.SearchAsync(communityId, query, cancellationToken);
    return Results.Ok(result);
})
.WithName("SearchCommunityProfiles");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();

public partial class Program { }
