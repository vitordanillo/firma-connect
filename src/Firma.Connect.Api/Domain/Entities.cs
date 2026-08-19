namespace Firma.Connect.Api.Domain;

public sealed class Community
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class User
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAt { get; set; }
}

public enum MembershipRole { Member, Admin }

public sealed class CommunityMembership
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid CommunityId { get; set; }
    public Guid UserId { get; set; }
    public MembershipRole Role { get; set; } = MembershipRole.Member;
    public DateTimeOffset JoinedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class CommunityInvitation
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid CommunityId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UsedAt { get; set; }

    public bool CanBeUsedAt(DateTimeOffset now) => UsedAt is null && ExpiresAt > now;
}

public sealed class Institution
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
}

public sealed class Skill
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
}

public sealed class Interest
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
}

public enum TeamSituation { LookingForTeam, HasTeam, NotLooking }

public sealed class Profile
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid CommunityId { get; set; }
    public Guid UserId { get; set; }
    public Guid? InstitutionId { get; set; }
    public string? Course { get; set; }
    public string? Headline { get; set; }
    public string? Bio { get; set; }
    public string? ProjectName { get; set; }
    public string? ProjectSummary { get; set; }
    public string? CanHelpWith { get; set; }
    public string? LookingFor { get; set; }
    public string? ContactUrl { get; set; }
    public bool AvailableForTeam { get; set; }
    public TeamSituation TeamSituation { get; set; } = TeamSituation.LookingForTeam;
    public bool VisibleInDirectory { get; set; } = true;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<ProfileSkill> Skills { get; set; } = new List<ProfileSkill>();
    public ICollection<ProfileInterest> Interests { get; set; } = new List<ProfileInterest>();

    public void SetTeamSituation(TeamSituation situation)
    {
        TeamSituation = situation;
        AvailableForTeam = situation == TeamSituation.LookingForTeam;
    }
}

public sealed class ProfileSkill
{
    public Guid ProfileId { get; set; }
    public Profile Profile { get; set; } = null!;
    public Guid SkillId { get; set; }
    public Skill Skill { get; set; } = null!;
}

public sealed class ProfileInterest
{
    public Guid ProfileId { get; set; }
    public Profile Profile { get; set; } = null!;
    public Guid InterestId { get; set; }
    public Interest Interest { get; set; } = null!;
}

public sealed class Team
{
    public const int MaximumMembers = 4;
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid CommunityId { get; set; }
    public Guid InstitutionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ProjectSummary { get; set; }
    public bool IsOpen { get; set; } = true;
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
    public ICollection<TeamDesiredSkill> DesiredSkills { get; set; } = new List<TeamDesiredSkill>();

    public bool HasOpenSpot => IsOpen && Members.Count < MaximumMembers;
    public bool AcceptsInstitution(Guid institutionId) => InstitutionId == institutionId;
}

public enum TeamMemberRole { Owner, Member }

public sealed class TeamMember
{
    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;
    public Guid CommunityId { get; set; }
    public Guid UserId { get; set; }
    public TeamMemberRole Role { get; set; } = TeamMemberRole.Member;
    public DateTimeOffset JoinedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class TeamDesiredSkill
{
    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;
    public Guid SkillId { get; set; }
    public Skill Skill { get; set; } = null!;
}

public enum TeamJoinRequestStatus { Pending, Accepted, Declined, Cancelled }

public sealed class TeamJoinRequest
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid TeamId { get; set; }
    public Guid RequesterProfileId { get; set; }
    public string? Note { get; set; }
    public TeamJoinRequestStatus Status { get; set; } = TeamJoinRequestStatus.Pending;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RespondedAt { get; set; }
}

public enum ConnectionStatus { Pending, Accepted, Declined, Cancelled }

public sealed class ConnectionRequest
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid CommunityId { get; set; }
    public Guid RequesterProfileId { get; set; }
    public Guid RecipientProfileId { get; set; }
    public string? Note { get; set; }
    public ConnectionStatus Status { get; set; } = ConnectionStatus.Pending;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RespondedAt { get; set; }
}
