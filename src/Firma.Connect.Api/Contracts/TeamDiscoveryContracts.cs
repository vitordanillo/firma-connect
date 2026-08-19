namespace Firma.Connect.Api.Contracts;

public sealed record TeamDiscoverySummary(
    Guid InstitutionId,
    string Institution,
    int Participants,
    int LookingForTeam,
    int OpenTeams,
    int AlreadyInTeam);
