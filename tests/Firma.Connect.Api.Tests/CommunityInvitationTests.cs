using Firma.Connect.Api.Domain;

namespace Firma.Connect.Api.Tests;

public class CommunityInvitationTests
{
    [Fact]
    public void Active_invitation_can_be_used_before_expiration()
    {
        var now = DateTimeOffset.UtcNow;
        var invitation = new CommunityInvitation { ExpiresAt = now.AddHours(1) };

        Assert.True(invitation.CanBeUsedAt(now));
    }

    [Fact]
    public void Used_invitation_cannot_be_used_again()
    {
        var now = DateTimeOffset.UtcNow;
        var invitation = new CommunityInvitation { ExpiresAt = now.AddHours(1), UsedAt = now };

        Assert.False(invitation.CanBeUsedAt(now));
    }
}
