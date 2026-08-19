using Firma.Connect.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Firma.Connect.Api.Data;

public sealed class BootstrapSeeder(
    FirmaDbContext db,
    IPasswordHasher<User> passwordHasher)
{
    public async Task SeedAsync(IConfiguration configuration, CancellationToken cancellationToken)
    {
        var email = configuration["Bootstrap:Email"]?.Trim().ToLowerInvariant();
        var password = configuration["Bootstrap:Password"];
        var displayName = configuration["Bootstrap:DisplayName"]?.Trim();
        var communityName = configuration["Bootstrap:CommunityName"]?.Trim();
        var communitySlug = configuration["Bootstrap:CommunitySlug"]?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(communityName) ||
            string.IsNullOrWhiteSpace(communitySlug))
            return;
        if (password.Length < 10)
            throw new InvalidOperationException("Bootstrap:Password deve ter pelo menos 10 caracteres.");

        var user = await db.Users.SingleOrDefaultAsync(item => item.Email == email, cancellationToken);
        if (user is null)
        {
            user = new User { Email = email, DisplayName = displayName };
            user.PasswordHash = passwordHasher.HashPassword(user, password);
            db.Users.Add(user);
        }

        var community = await db.Communities.SingleOrDefaultAsync(item => item.Slug == communitySlug, cancellationToken);
        if (community is null)
        {
            community = new Community { Name = communityName, Slug = communitySlug };
            db.Communities.Add(community);
        }

        await db.SaveChangesAsync(cancellationToken);

        var membership = await db.CommunityMemberships.SingleOrDefaultAsync(
            item => item.CommunityId == community.Id && item.UserId == user.Id, cancellationToken);
        if (membership is null)
            db.CommunityMemberships.Add(new CommunityMembership
            {
                CommunityId = community.Id,
                UserId = user.Id,
                Role = MembershipRole.Admin
            });
        else
            membership.Role = MembershipRole.Admin;

        await db.SaveChangesAsync(cancellationToken);
    }
}
