namespace Firma.Connect.Api.Contracts;

public sealed record CreateTeamRequest(string Name, string? ProjectSummary, IReadOnlyCollection<string> DesiredSkills);
public sealed record CreateTeamJoinRequest(string? Note);
public sealed record TeamSearchQuery(Guid? InstitutionId, string? Skill, bool OpenOnly = true, bool SameInstitutionFirst = true, int Page = 1, int PageSize = 20);
public sealed record TeamDirectoryItem(Guid Id, string Name, string Institution, string? ProjectSummary, bool IsOpen, int MemberCount, int OpenSpots, IReadOnlyCollection<string> DesiredSkills);
public sealed record TeamSearchResponse(IReadOnlyCollection<TeamDirectoryItem> Items, int Total);
public sealed record OwnTeamResponse(TeamDirectoryItem Team, string Role);
public sealed record TeamJoinRequestItem(Guid Id, Guid TeamId, Guid RequesterProfileId, string RequesterName, string? Note, string Status, DateTimeOffset CreatedAt);
