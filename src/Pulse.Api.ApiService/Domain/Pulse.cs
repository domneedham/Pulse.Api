namespace Pulse.Api.ApiService.Domain;

/// <summary>
/// A single signal sent from one partner to the other — the core interaction. The <see cref="Type"/>
/// determines which detail row carries the payload (mood/need/thought/touch); detail lives in its own
/// table rather than a generic JSON blob. Pulses are append-only history; they make up the timeline.
/// </summary>
public class Pulse
{
    public Guid Id { get; set; }
    public Guid ConnectionId { get; set; }

    /// <summary>The partner who sent it (the recipient is the other member of the connection).</summary>
    public Guid SenderId { get; set; }

    public PulseType Type { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Connection Connection { get; set; } = null!;
    public User Sender { get; set; } = null!;

    // Exactly one of these is populated, matching Type. One-to-one detail rows keyed by PulseId.
    public PulseMood? Mood { get; set; }
    public PulseNeed? Need { get; set; }
    public PulseThought? Thought { get; set; }
}

/// <summary>Mood payload for a <see cref="PulseType.Mood"/> pulse.</summary>
public class PulseMood
{
    public Guid PulseId { get; set; }
    public MoodType MoodType { get; set; }

    public Pulse Pulse { get; set; } = null!;
}

/// <summary>Need payload for a <see cref="PulseType.Need"/> pulse.</summary>
public class PulseNeed
{
    public Guid PulseId { get; set; }
    public NeedType NeedType { get; set; }

    public Pulse Pulse { get; set; } = null!;
}

/// <summary>Thought payload for a <see cref="PulseType.Thought"/> pulse — a short free-text message (max 50 chars).</summary>
public class PulseThought
{
    public Guid PulseId { get; set; }
    public required string Message { get; set; }

    public Pulse Pulse { get; set; } = null!;
}
