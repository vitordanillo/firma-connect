using Firma.Connect.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Firma.Connect.Api.Data;

public sealed class FirmaDbContext(DbContextOptions<FirmaDbContext> options) : DbContext(options)
{
    public DbSet<Community> Communities => Set<Community>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Institution> Institutions => Set<Institution>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<ConnectionRequest> ConnectionRequests => Set<ConnectionRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");
        modelBuilder.Entity<Community>().HasIndex(x => x.Slug).IsUnique();
        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Institution>().HasIndex(x => x.NormalizedName).IsUnique();
        modelBuilder.Entity<Profile>().HasIndex(x => new { x.CommunityId, x.UserId }).IsUnique();
        modelBuilder.Entity<ConnectionRequest>().HasIndex(x => new { x.CommunityId, x.RequesterProfileId, x.RecipientProfileId }).IsUnique();
        modelBuilder.Entity<ConnectionRequest>().Property(x => x.Status).HasConversion<string>();
    }
}
