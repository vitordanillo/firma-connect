using Firma.Connect.Api.Data;
using Firma.Connect.Api.Contracts;
using Firma.Connect.Api.Domain;
using Firma.Connect.Api.Features.Auth;
using Firma.Connect.Api.Features.Communities;
using Firma.Connect.Api.Features.Institutions;
using Firma.Connect.Api.Features.Profiles;
using Firma.Connect.Api.Features.Teams;
using Firma.Connect.Api.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

builder.Services.AddDbContext<FirmaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"))
           .UseSnakeCaseNamingConvention());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks().AddDbContextCheck<FirmaDbContext>();
builder.Services.AddScoped<ProfileDirectoryService>();
builder.Services.AddScoped<ProfileManagementService>();
builder.Services.AddScoped<InstitutionDirectoryService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<CommunityAccessService>();
builder.Services.AddScoped<InvitationService>();
builder.Services.AddScoped<TeamService>();
builder.Services.AddScoped<TeamDiscoveryService>();
builder.Services.AddScoped<BootstrapSeeder>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Configuração JWT ausente.");
if (Encoding.UTF8.GetByteCount(jwtOptions.Key) < 32)
    throw new InvalidOperationException("Jwt:Key deve ter pelo menos 32 bytes.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

using (var bootstrapScope = app.Services.CreateScope())
{
    var bootstrap = bootstrapScope.ServiceProvider.GetRequiredService<BootstrapSeeder>();
    await bootstrap.SeedAsync(app.Configuration, CancellationToken.None);
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");

app.MapPost("/api/auth/register", async (
    RegisterRequest request,
    AuthService auth,
    CancellationToken cancellationToken) =>
{
    var result = await auth.RegisterAsync(request, cancellationToken);
    return result.Succeeded ? Results.Ok(result.Response) : Results.BadRequest(new { error = result.Error });
});

app.MapPost("/api/auth/login", async (
    LoginRequest request,
    AuthService auth,
    CancellationToken cancellationToken) =>
{
    var result = await auth.LoginAsync(request, cancellationToken);
    return result.Succeeded ? Results.Ok(result.Response) : Results.Json(new { error = result.Error }, statusCode: 401);
});

app.MapPost("/api/communities/{communityId:guid}/invitations", async (
    Guid communityId,
    CreateInvitationRequest request,
    HttpContext context,
    CommunityAccessService access,
    InvitationService invitations,
    CancellationToken cancellationToken) =>
{
    var userId = context.User.GetUserId();
    if (userId is null)
        return Results.Unauthorized();
    if (!await access.IsAdminAsync(communityId, userId.Value, cancellationToken))
        return Results.Forbid();

    var invitation = await invitations.CreateAsync(communityId, userId.Value, request, cancellationToken);
    return invitation is null
        ? Results.BadRequest(new { error = "E-mail inválido." })
        : Results.Ok(invitation);
}).RequireAuthorization();

app.MapGet("/api/institutions", async (
    [AsParameters] InstitutionSearchQuery query,
    InstitutionDirectoryService institutions,
    CancellationToken cancellationToken) =>
{
    var result = await institutions.SearchAsync(query, cancellationToken);
    return Results.Ok(result);
}).RequireAuthorization();

app.MapGet("/api/communities/{communityId:guid}/profiles/me", async (
    Guid communityId,
    HttpContext context,
    CommunityAccessService access,
    ProfileManagementService profiles,
    CancellationToken cancellationToken) =>
{
    var userId = context.User.GetUserId();
    if (userId is null)
        return Results.Unauthorized();
    if (!await access.IsMemberAsync(communityId, userId.Value, cancellationToken))
        return Results.Forbid();

    var profile = await profiles.GetAsync(communityId, userId.Value, cancellationToken);
    return profile is null ? Results.NotFound() : Results.Ok(profile);
}).RequireAuthorization();

app.MapPut("/api/communities/{communityId:guid}/profiles/me", async (
    Guid communityId,
    UpsertProfileRequest request,
    HttpContext context,
    CommunityAccessService access,
    ProfileManagementService profiles,
    CancellationToken cancellationToken) =>
{
    var userId = context.User.GetUserId();
    if (userId is null)
        return Results.Unauthorized();
    if (!await access.IsMemberAsync(communityId, userId.Value, cancellationToken))
        return Results.Forbid();

    var result = await profiles.UpsertAsync(communityId, userId.Value, request, cancellationToken);
    return result.Succeeded
        ? Results.Ok(result.Profile)
        : Results.BadRequest(new { error = result.Error });
}).RequireAuthorization();

app.MapDelete("/api/communities/{communityId:guid}/profiles/me", async (
    Guid communityId,
    HttpContext context,
    CommunityAccessService access,
    ProfileManagementService profiles,
    CancellationToken cancellationToken) =>
{
    var userId = context.User.GetUserId();
    if (userId is null)
        return Results.Unauthorized();
    if (!await access.IsMemberAsync(communityId, userId.Value, cancellationToken))
        return Results.Forbid();

    var deleted = await profiles.DeleteAsync(communityId, userId.Value, cancellationToken);
    return deleted ? Results.NoContent() : Results.NotFound();
}).RequireAuthorization();

app.MapGet("/api/communities/{communityId:guid}/teams", async (
    Guid communityId,
    [AsParameters] TeamSearchQuery query,
    HttpContext context,
    CommunityAccessService access,
    TeamService teams,
    CancellationToken cancellationToken) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();
    if (!await access.IsMemberAsync(communityId, userId.Value, cancellationToken)) return Results.Forbid();
    return Results.Ok(await teams.SearchAsync(communityId, userId.Value, query, cancellationToken));
}).RequireAuthorization();

app.MapGet("/api/communities/{communityId:guid}/team-discovery/summary", async (
    Guid communityId,
    HttpContext context,
    CommunityAccessService access,
    TeamDiscoveryService discovery,
    CancellationToken cancellationToken) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();
    if (!await access.IsMemberAsync(communityId, userId.Value, cancellationToken)) return Results.Forbid();
    var summary = await discovery.GetSummaryAsync(communityId, userId.Value, cancellationToken);
    return summary is null
        ? Results.BadRequest(new { error = "Preencha sua instituição para encontrar sua equipe." })
        : Results.Ok(summary);
}).RequireAuthorization();

app.MapGet("/api/communities/{communityId:guid}/teams/me", async (
    Guid communityId,
    HttpContext context,
    CommunityAccessService access,
    TeamService teams,
    CancellationToken cancellationToken) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();
    if (!await access.IsMemberAsync(communityId, userId.Value, cancellationToken)) return Results.Forbid();
    var team = await teams.GetOwnTeamAsync(communityId, userId.Value, cancellationToken);
    return team is null ? Results.NotFound() : Results.Ok(team);
}).RequireAuthorization();

app.MapPost("/api/communities/{communityId:guid}/teams", async (
    Guid communityId,
    CreateTeamRequest request,
    HttpContext context,
    CommunityAccessService access,
    TeamService teams,
    CancellationToken cancellationToken) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();
    if (!await access.IsMemberAsync(communityId, userId.Value, cancellationToken)) return Results.Forbid();
    var result = await teams.CreateAsync(communityId, userId.Value, request, cancellationToken);
    return result.Succeeded ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
}).RequireAuthorization();

app.MapPost("/api/communities/{communityId:guid}/teams/{teamId:guid}/requests", async (
    Guid communityId,
    Guid teamId,
    CreateTeamJoinRequest request,
    HttpContext context,
    CommunityAccessService access,
    TeamService teams,
    CancellationToken cancellationToken) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();
    if (!await access.IsMemberAsync(communityId, userId.Value, cancellationToken)) return Results.Forbid();
    var result = await teams.RequestToJoinAsync(communityId, teamId, userId.Value, request, cancellationToken);
    return result.Succeeded ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
}).RequireAuthorization();

app.MapGet("/api/communities/{communityId:guid}/teams/{teamId:guid}/requests", async (
    Guid communityId,
    Guid teamId,
    HttpContext context,
    TeamService teams,
    CancellationToken cancellationToken) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();
    var result = await teams.ListRequestsAsync(communityId, teamId, userId.Value, cancellationToken);
    return result.Succeeded ? Results.Ok(result.Value) : Results.Forbid();
}).RequireAuthorization();

app.MapPost("/api/communities/{communityId:guid}/team-requests/{requestId:guid}/accept", async (
    Guid communityId,
    Guid requestId,
    HttpContext context,
    TeamService teams,
    CancellationToken cancellationToken) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();
    var result = await teams.RespondAsync(communityId, requestId, userId.Value, true, cancellationToken);
    return result.Succeeded ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
}).RequireAuthorization();

app.MapPost("/api/communities/{communityId:guid}/team-requests/{requestId:guid}/decline", async (
    Guid communityId,
    Guid requestId,
    HttpContext context,
    TeamService teams,
    CancellationToken cancellationToken) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();
    var result = await teams.RespondAsync(communityId, requestId, userId.Value, false, cancellationToken);
    return result.Succeeded ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
}).RequireAuthorization();

app.MapGet("/api/communities/{communityId:guid}/profiles", async (
    Guid communityId,
    [AsParameters] ProfileSearchQuery query,
    HttpContext context,
    CommunityAccessService access,
    ProfileDirectoryService directory,
    CancellationToken cancellationToken) =>
{
    var userId = context.User.GetUserId();
    if (userId is null)
        return Results.Unauthorized();
    if (!await access.IsMemberAsync(communityId, userId.Value, cancellationToken))
        return Results.Forbid();

    var result = await directory.SearchAsync(communityId, userId.Value, query, cancellationToken);
    return Results.Ok(result);
})
.WithName("SearchCommunityProfiles")
.RequireAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();

public partial class Program { }
