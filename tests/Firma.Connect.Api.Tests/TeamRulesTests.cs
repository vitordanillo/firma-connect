using Firma.Connect.Api.Domain;

namespace Firma.Connect.Api.Tests;

public class TeamRulesTests
{
    [Fact]
    public void Team_closes_after_four_members()
    {
        var team = new Team();
        for (var index = 0; index < Team.MaximumMembers; index++)
            team.Members.Add(new TeamMember { CommunityId = Guid.NewGuid(), UserId = Guid.NewGuid() });

        Assert.False(team.HasOpenSpot);
    }

    [Fact]
    public void Team_only_accepts_its_own_institution()
    {
        var institutionId = Guid.NewGuid();
        var team = new Team { InstitutionId = institutionId };

        Assert.True(team.AcceptsInstitution(institutionId));
        Assert.False(team.AcceptsInstitution(Guid.NewGuid()));
    }

    [Theory]
    [InlineData(TeamSituation.LookingForTeam, true)]
    [InlineData(TeamSituation.HasTeam, false)]
    [InlineData(TeamSituation.NotLooking, false)]
    public void Profile_availability_follows_team_situation(TeamSituation situation, bool expected)
    {
        var profile = new Profile();
        profile.SetTeamSituation(situation);

        Assert.Equal(expected, profile.AvailableForTeam);
    }
}
