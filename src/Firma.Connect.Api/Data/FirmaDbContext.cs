using Firma.Connect.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Firma.Connect.Api.Data;

public sealed class FirmaDbContext(DbContextOptions<FirmaDbContext> options) : DbContext(options)
{
    public DbSet<Community> Communities => Set<Community>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Institution> Institutions => Set<Institution>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<Interest> Interests => Set<Interest>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<ProfileSkill> ProfileSkills => Set<ProfileSkill>();
    public DbSet<ProfileInterest> ProfileInterests => Set<ProfileInterest>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<TeamDesiredSkill> TeamDesiredSkills => Set<TeamDesiredSkill>();
    public DbSet<TeamJoinRequest> TeamJoinRequests => Set<TeamJoinRequest>();
    public DbSet<CommunityMembership> CommunityMemberships => Set<CommunityMembership>();
    public DbSet<CommunityInvitation> CommunityInvitations => Set<CommunityInvitation>();
    public DbSet<ConnectionRequest> ConnectionRequests => Set<ConnectionRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");
        modelBuilder.Entity<Community>().HasIndex(x => x.Slug).IsUnique();
        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Institution>().HasIndex(x => x.NormalizedName).IsUnique();
        modelBuilder.Entity<Skill>().HasIndex(x => x.NormalizedName).IsUnique();
        modelBuilder.Entity<Interest>().HasIndex(x => x.NormalizedName).IsUnique();
        modelBuilder.Entity<CommunityMembership>().ToTable("communities_memberships");
        modelBuilder.Entity<CommunityMembership>().HasIndex(x => new { x.CommunityId, x.UserId }).IsUnique();
        modelBuilder.Entity<CommunityMembership>().Property(x => x.Role).HasConversion<string>();
        modelBuilder.Entity<CommunityInvitation>().ToTable("community_invitations");
        modelBuilder.Entity<CommunityInvitation>().HasIndex(x => x.TokenHash).IsUnique();
        modelBuilder.Entity<CommunityInvitation>().HasIndex(x => new { x.CommunityId, x.Email });
        modelBuilder.Entity<Profile>().HasIndex(x => new { x.CommunityId, x.UserId }).IsUnique();
        modelBuilder.Entity<Profile>().Property(x => x.TeamSituation).HasConversion<string>();
        modelBuilder.Entity<ProfileSkill>().ToTable("profile_skills");
        modelBuilder.Entity<ProfileSkill>().HasKey(x => new { x.ProfileId, x.SkillId });
        modelBuilder.Entity<ProfileInterest>().ToTable("profile_interests");
        modelBuilder.Entity<ProfileInterest>().HasKey(x => new { x.ProfileId, x.InterestId });
        modelBuilder.Entity<TeamMember>().ToTable("team_members");
        modelBuilder.Entity<TeamMember>().HasKey(x => new { x.TeamId, x.UserId });
        modelBuilder.Entity<TeamMember>().HasIndex(x => new { x.CommunityId, x.UserId }).IsUnique();
        modelBuilder.Entity<TeamMember>().Property(x => x.Role).HasConversion<string>();
        modelBuilder.Entity<TeamDesiredSkill>().ToTable("team_desired_skills");
        modelBuilder.Entity<TeamDesiredSkill>().HasKey(x => new { x.TeamId, x.SkillId });
        modelBuilder.Entity<TeamJoinRequest>().ToTable("team_join_requests");
        modelBuilder.Entity<TeamJoinRequest>().Property(x => x.Status).HasConversion<string>();
        modelBuilder.Entity<TeamJoinRequest>().HasIndex(x => new { x.TeamId, x.RequesterProfileId }).IsUnique();
        modelBuilder.Entity<ConnectionRequest>().HasIndex(x => new { x.CommunityId, x.RequesterProfileId, x.RecipientProfileId }).IsUnique();
        modelBuilder.Entity<ConnectionRequest>().Property(x => x.Status).HasConversion<string>();
    }
}
