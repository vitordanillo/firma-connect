using System.Data;
using Firma.Connect.Api.Contracts;
using Firma.Connect.Api.Data;
using Firma.Connect.Api.Domain;
using Firma.Connect.Api.Features.Institutions;
using Microsoft.EntityFrameworkCore;

namespace Firma.Connect.Api.Features.Teams;

public sealed record TeamResult<T>(T? Value, string? Error) where T : class
{
    public bool Succeeded => Value is not null;
    public static TeamResult<T> Success(T value) => new(value, null);
    public static TeamResult<T> Failure(string error) => new(null, error);
}

public sealed class TeamService(FirmaDbContext db, TimeProvider timeProvider)
{
    private const int MaxPageSize = 50;

    public async Task<TeamResult<TeamDirectoryItem>> CreateAsync(
        Guid communityId, Guid userId, CreateTeamRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 120)
            return TeamResult<TeamDirectoryItem>.Failure("O nome da equipe deve ter entre 1 e 120 caracteres.");
        if (request.ProjectSummary?.Trim().Length > 500)
            return TeamResult<TeamDirectoryItem>.Failure("O resumo deve ter no máximo 500 caracteres.");
        var requestedSkills = request.DesiredSkills ?? Array.Empty<string>();
        if (requestedSkills.Count > 10 || requestedSkills.Any(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length is < 2 or > 60))
            return TeamResult<TeamDirectoryItem>.Failure("Informe até 10 competências com 2 a 60 caracteres.");

        var profile = await db.Profiles.SingleOrDefaultAsync(
            item => item.CommunityId == communityId && item.UserId == userId, cancellationToken);
        if (profile?.InstitutionId is null)
            return TeamResult<TeamDirectoryItem>.Failure("Preencha sua instituição antes de criar uma equipe.");
        if (await UserHasTeamAsync(communityId, userId, cancellationToken))
            return TeamResult<TeamDirectoryItem>.Failure("Você já participa de uma equipe nesta comunidade.");

        var team = new Team
        {
            CommunityId = communityId,
            InstitutionId = profile.InstitutionId.Value,
            Name = request.Name.Trim(),
            ProjectSummary = Clean(request.ProjectSummary),
            CreatedByUserId = userId,
            UpdatedAt = timeProvider.GetUtcNow()
        };
        team.Members.Add(new TeamMember { CommunityId = communityId, UserId = userId, Role = TeamMemberRole.Owner });
        foreach (var skill in await ResolveSkillsAsync(requestedSkills, cancellationToken))
            team.DesiredSkills.Add(new TeamDesiredSkill { Skill = skill });
        db.Teams.Add(team);
        profile.SetTeamSituation(TeamSituation.HasTeam);
        await db.SaveChangesAsync(cancellationToken);

        return TeamResult<TeamDirectoryItem>.Success(await MapTeamAsync(team.Id, cancellationToken));
    }

    public async Task<TeamSearchResponse> SearchAsync(
        Guid communityId, Guid userId, TeamSearchQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var requesterInstitutionId = await db.Profiles.AsNoTracking()
            .Where(profile => profile.CommunityId == communityId && profile.UserId == userId)
            .Select(profile => profile.InstitutionId).SingleOrDefaultAsync(cancellationToken);
        var teams = db.Teams.AsNoTracking().Where(team => team.CommunityId == communityId);
        if (query.InstitutionId is not null) teams = teams.Where(team => team.InstitutionId == query.InstitutionId);
        if (query.OpenOnly) teams = teams.Where(team => team.IsOpen && team.Members.Count < Team.MaximumMembers);
        if (!string.IsNullOrWhiteSpace(query.Skill))
        {
            var skill = InstitutionDirectoryService.Normalize(query.Skill);
            teams = teams.Where(team => team.DesiredSkills.Any(item => item.Skill.NormalizedName == skill));
        }

        var total = await teams.CountAsync(cancellationToken);
        var ordered = query.SameInstitutionFirst && requesterInstitutionId is not null
            ? teams.OrderByDescending(team => team.InstitutionId == requesterInstitutionId).ThenBy(team => team.Name)
            : teams.OrderBy(team => team.Name);
        var ids = await ordered.Skip((page - 1) * pageSize).Take(pageSize).Select(team => team.Id).ToArrayAsync(cancellationToken);
        var items = new List<TeamDirectoryItem>(ids.Length);
        foreach (var id in ids) items.Add(await MapTeamAsync(id, cancellationToken));
        return new TeamSearchResponse(items, total);
    }

    public async Task<OwnTeamResponse?> GetOwnTeamAsync(
        Guid communityId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var membership = await db.TeamMembers.AsNoTracking().SingleOrDefaultAsync(
            item => item.CommunityId == communityId && item.UserId == userId,
            cancellationToken);
        if (membership is null) return null;

        return new OwnTeamResponse(
            await MapTeamAsync(membership.TeamId, cancellationToken),
            membership.Role.ToString().ToLowerInvariant());
    }

    public async Task<TeamResult<TeamJoinRequestItem>> RequestToJoinAsync(
        Guid communityId, Guid teamId, Guid userId, CreateTeamJoinRequest request, CancellationToken cancellationToken)
    {
        if (request.Note?.Trim().Length > 300)
            return TeamResult<TeamJoinRequestItem>.Failure("A mensagem deve ter no máximo 300 caracteres.");
        var team = await db.Teams.Include(item => item.Members).SingleOrDefaultAsync(
            item => item.Id == teamId && item.CommunityId == communityId, cancellationToken);
        var profile = await db.Profiles.SingleOrDefaultAsync(
            item => item.CommunityId == communityId && item.UserId == userId, cancellationToken);
        if (team is null || profile is null) return TeamResult<TeamJoinRequestItem>.Failure("Equipe ou perfil não encontrado.");
        if (profile.InstitutionId is null || !team.AcceptsInstitution(profile.InstitutionId.Value)) return TeamResult<TeamJoinRequestItem>.Failure("A equipe pertence a outra instituição.");
        if (!team.HasOpenSpot) return TeamResult<TeamJoinRequestItem>.Failure("A equipe não possui vagas abertas.");
        if (await UserHasTeamAsync(communityId, userId, cancellationToken)) return TeamResult<TeamJoinRequestItem>.Failure("Você já participa de uma equipe.");
        if (await db.TeamJoinRequests.AnyAsync(item => item.TeamId == teamId && item.RequesterProfileId == profile.Id, cancellationToken))
            return TeamResult<TeamJoinRequestItem>.Failure("Você já enviou uma solicitação para esta equipe.");

        var joinRequest = new TeamJoinRequest { TeamId = teamId, RequesterProfileId = profile.Id, Note = Clean(request.Note) };
        db.TeamJoinRequests.Add(joinRequest);
        await db.SaveChangesAsync(cancellationToken);
        var user = await db.Users.AsNoTracking().SingleAsync(item => item.Id == userId, cancellationToken);
        return TeamResult<TeamJoinRequestItem>.Success(MapRequest(joinRequest, user.DisplayName));
    }

    public async Task<TeamResult<TeamJoinRequestItem>> RespondAsync(
        Guid communityId, Guid requestId, Guid ownerUserId, bool accept, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var joinRequest = await db.TeamJoinRequests.SingleOrDefaultAsync(item => item.Id == requestId, cancellationToken);
        if (joinRequest is null) return TeamResult<TeamJoinRequestItem>.Failure("Solicitação não encontrada.");
        var team = await db.Teams.Include(item => item.Members).SingleOrDefaultAsync(
            item => item.Id == joinRequest.TeamId && item.CommunityId == communityId, cancellationToken);
        if (team is null || team.Members.All(member => member.UserId != ownerUserId || member.Role != TeamMemberRole.Owner))
            return TeamResult<TeamJoinRequestItem>.Failure("Somente o responsável pela equipe pode responder.");
        if (joinRequest.Status != TeamJoinRequestStatus.Pending)
            return TeamResult<TeamJoinRequestItem>.Failure("A solicitação já foi respondida.");

        var profile = await db.Profiles.SingleAsync(item => item.Id == joinRequest.RequesterProfileId, cancellationToken);
        var user = await db.Users.SingleAsync(item => item.Id == profile.UserId, cancellationToken);
        if (accept)
        {
            if (!team.HasOpenSpot) return TeamResult<TeamJoinRequestItem>.Failure("A equipe não possui vagas.");
            if (profile.InstitutionId is null || !team.AcceptsInstitution(profile.InstitutionId.Value)) return TeamResult<TeamJoinRequestItem>.Failure("O participante pertence a outra instituição.");
            if (await UserHasTeamAsync(communityId, profile.UserId, cancellationToken)) return TeamResult<TeamJoinRequestItem>.Failure("O participante já entrou em outra equipe.");
            team.Members.Add(new TeamMember { CommunityId = communityId, UserId = profile.UserId });
            profile.SetTeamSituation(TeamSituation.HasTeam);
            joinRequest.Status = TeamJoinRequestStatus.Accepted;
            if (team.Members.Count >= Team.MaximumMembers) team.IsOpen = false;
        }
        else joinRequest.Status = TeamJoinRequestStatus.Declined;

        joinRequest.RespondedAt = timeProvider.GetUtcNow();
        team.UpdatedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return TeamResult<TeamJoinRequestItem>.Success(MapRequest(joinRequest, user.DisplayName));
    }

    public async Task<TeamResult<IReadOnlyCollection<TeamJoinRequestItem>>> ListRequestsAsync(
        Guid communityId, Guid teamId, Guid ownerUserId, CancellationToken cancellationToken)
    {
        var isOwner = await db.TeamMembers.AnyAsync(item => item.TeamId == teamId && item.UserId == ownerUserId && item.Role == TeamMemberRole.Owner, cancellationToken);
        var belongsToCommunity = await db.Teams.AnyAsync(item => item.Id == teamId && item.CommunityId == communityId, cancellationToken);
        if (!isOwner || !belongsToCommunity) return TeamResult<IReadOnlyCollection<TeamJoinRequestItem>>.Failure("Acesso não autorizado à equipe.");
        var items = await db.TeamJoinRequests.AsNoTracking().Where(item => item.TeamId == teamId)
            .Join(db.Profiles.AsNoTracking(), request => request.RequesterProfileId, profile => profile.Id, (request, profile) => new { request, profile })
            .Join(db.Users.AsNoTracking(), value => value.profile.UserId, user => user.Id, (value, user) => new { value.request, user.DisplayName })
            .OrderByDescending(value => value.request.CreatedAt)
            .Select(value => new TeamJoinRequestItem(value.request.Id, value.request.TeamId, value.request.RequesterProfileId, value.DisplayName, value.request.Note, value.request.Status.ToString().ToLower(), value.request.CreatedAt))
            .ToArrayAsync(cancellationToken);
        return TeamResult<IReadOnlyCollection<TeamJoinRequestItem>>.Success(items);
    }

    private async Task<TeamDirectoryItem> MapTeamAsync(Guid teamId, CancellationToken cancellationToken)
    {
        var team = await db.Teams.AsNoTracking().Include(item => item.Members).Include(item => item.DesiredSkills).ThenInclude(item => item.Skill).SingleAsync(item => item.Id == teamId, cancellationToken);
        var institution = await db.Institutions.AsNoTracking().Where(item => item.Id == team.InstitutionId).Select(item => item.Name).SingleAsync(cancellationToken);
        return new TeamDirectoryItem(team.Id, team.Name, institution, team.ProjectSummary, team.IsOpen, team.Members.Count,
            Math.Max(0, Team.MaximumMembers - team.Members.Count), team.DesiredSkills.Select(item => item.Skill.Name).Order().ToArray());
    }

    private async Task<IReadOnlyCollection<Skill>> ResolveSkillsAsync(IReadOnlyCollection<string> values, CancellationToken cancellationToken)
    {
        var normalized = values.Select(InstitutionDirectoryService.Normalize).Distinct().ToArray();
        var existing = await db.Skills.Where(item => normalized.Contains(item.NormalizedName)).ToListAsync(cancellationToken);
        foreach (var item in normalized.Where(value => existing.All(skill => skill.NormalizedName != value)))
            existing.Add(new Skill { Name = values.First(value => InstitutionDirectoryService.Normalize(value) == item).Trim(), NormalizedName = item });
        return existing;
    }

    private Task<bool> UserHasTeamAsync(Guid communityId, Guid userId, CancellationToken cancellationToken)
        => db.TeamMembers.AnyAsync(member => member.CommunityId == communityId && member.UserId == userId, cancellationToken);

    private static TeamJoinRequestItem MapRequest(TeamJoinRequest request, string name)
        => new(request.Id, request.TeamId, request.RequesterProfileId, name, request.Note, request.Status.ToString().ToLowerInvariant(), request.CreatedAt);
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
