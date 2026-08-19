namespace Firma.Connect.Api.Contracts;

public sealed record RegisterRequest(string InvitationToken, string DisplayName, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record CommunityAccessItem(Guid Id, string Name, string Role);
public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    Guid UserId,
    string DisplayName,
    IReadOnlyCollection<CommunityAccessItem> Communities);
public sealed record CreateInvitationRequest(string Email);
public sealed record InvitationResponse(Guid Id, string Token, DateTimeOffset ExpiresAt);
