using Pulse.Api.ApiService.Domain;

namespace Pulse.Api.ApiService.Contracts;

/// <summary>The other member of a connection, as the app renders them.</summary>
public record PartnerDto(Guid Id, string DisplayName, string? AvatarUrl, string? Username);

/// <summary>
/// The caller's current connection. While Pending, <see cref="Partner"/> is null and
/// <see cref="InviteCode"/> carries the code to share. Once Active, the partner is set and the
/// code is gone.
/// </summary>
public record ConnectionDto(
    Guid Id,
    ConnectionStatus Status,
    string? InviteCode,
    PartnerDto? Partner,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ConnectedAt);

/// <summary>Body for joining a partner's connection via the code they shared.</summary>
public record AcceptInviteRequest(string InviteCode);
