namespace Pulse.Api.ApiService.Domain;

/// <summary>
/// The private link between two people — the heart of Pulse. A user has at most one active or
/// pending connection at a time. Created when one partner issues an invite (status Pending, only
/// <see cref="UserAId"/> set); becomes Active when the other partner accepts via the invite code
/// (<see cref="UserBId"/> set). All pulses belong to a connection.
/// </summary>
public class Connection
{
    public Guid Id { get; set; }

    /// <summary>The inviter — set at creation.</summary>
    public Guid UserAId { get; set; }

    /// <summary>The invitee — null while Pending, set when the invite is accepted.</summary>
    public Guid? UserBId { get; set; }

    public ConnectionStatus Status { get; set; }

    /// <summary>
    /// Short human-typeable code the inviter shares so their partner can join. Unique among
    /// connections that still have an outstanding invite. Cleared once accepted.
    /// </summary>
    public string? InviteCode { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>When the invite was accepted and the connection went Active.</summary>
    public DateTimeOffset? ConnectedAt { get; set; }

    /// <summary>
    /// The couple's reference IANA timezone (e.g. "Europe/London"), used to decide which calendar day
    /// "today's Moment" falls on so both partners agree regardless of where each one is. Defaulted from
    /// the inviter's profile timezone when the invite is accepted; falls back to UTC.
    /// </summary>
    public string Timezone { get; set; } = "Etc/UTC";

    public User UserA { get; set; } = null!;
    public User? UserB { get; set; }

    public ICollection<Pulse> Pulses { get; set; } = [];

    /// <summary>True for an active link with both partners present.</summary>
    public bool IsActive => Status == ConnectionStatus.Active && UserBId is not null;

    /// <summary>The id of the partner of <paramref name="userId"/> within this connection, or null.</summary>
    public Guid? PartnerOf(Guid userId) =>
        userId == UserAId ? UserBId
        : userId == UserBId ? UserAId
        : null;

    /// <summary>Whether <paramref name="userId"/> is one of the two members.</summary>
    public bool Includes(Guid userId) => userId == UserAId || userId == UserBId;
}
