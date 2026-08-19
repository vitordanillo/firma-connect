using Firma.Connect.Api.Data;
using Firma.Connect.Api.Contracts;
using Firma.Connect.Api.Domain;
using Firma.Connect.Api.Features.Auth;
using Firma.Connect.Api.Features.Communities;
using Firma.Connect.Api.Features.Institutions;
using Firma.Connect.Api.Features.Profiles;
using Firma.Connect.Api.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

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

    var result = await directory.SearchAsync(communityId, query, cancellationToken);
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
