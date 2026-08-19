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

public sealed class Profile
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid CommunityId { get; set; }
    public Guid UserId { get; set; }
    public Guid? InstitutionId { get; set; }
    public string? Course { get; set; }
    public string? Headline { get; set; }
    public string? Bio { get; set; }
    public string? ContactUrl { get; set; }
    public bool AvailableForTeam { get; set; }
    public bool VisibleInDirectory { get; set; } = true;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
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
