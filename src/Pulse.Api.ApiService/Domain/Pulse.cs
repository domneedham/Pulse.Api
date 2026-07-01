namespace Pulse.Api.ApiService.Domain;

/// <summary>
/// A single signal sent from one partner to the other — the core interaction. The <see cref="Type"/>
/// selects which 1:1 detail row carries the payload (mood / need / thought = a phrase + emoji; touch =
/// a hand-drawn doodle stored as vector strokes). Keeping a detail table per category leaves room for
/// category-specific features. Pulses are append-only history; they make up the timeline.
/// </summary>
public class Pulse
{
    public Guid Id { get; set; }
    public Guid ConnectionId { get; set; }

    /// <summary>The partner who sent it (the recipient is the other member of the connection).</summary>
    public Guid SenderId { get; set; }

    public PulseType Type { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Whether a member has starred this pulse for the favourites view in the Pulses tab.</summary>
    public bool IsFavorite { get; set; }

    /// <summary>
    /// An optional emoji the recipient reacted with (e.g. ❤️/😂). One reaction per pulse — the partner
    /// taps "React" on the pulse detail. Null when not yet reacted to.
    /// </summary>
    public string? Reaction { get; set; }

    /// <summary>
    /// An optional short note the sender added when sending (≤ 80 chars), shown on the Trail row and the
    /// signal detail. Null when no note was attached.
    /// </summary>
    public string? Note { get; set; }

    public Connection Connection { get; set; } = null!;
    public User Sender { get; set; } = null!;

    // Exactly one is populated, matching Type. One-to-one detail rows keyed by PulseId.
    public PulseMood? Mood { get; set; }
    public PulseNeed? Need { get; set; }
    public PulseThought? Thought { get; set; }
    public PulseTouch? Touch { get; set; }

    /// <summary>The phrase + emoji for whichever text detail row is set (mood/need/thought); null for touch.</summary>
    public IPulsePhrase? Phrase => (IPulsePhrase?)Mood ?? (IPulsePhrase?)Need ?? Thought;
}

/// <summary>
/// Shared shape of the text-based pulse details (mood / need / thought): a phrase + an emoji. Lets the
/// service map any of them to a DTO uniformly while the tables stay separate for future divergence.
/// </summary>
public interface IPulsePhrase
{
    string Text { get; }
    string? Emoji { get; }
}

/// <summary>Mood payload — the phrase the sender chose (e.g. "Feeling great") + emoji.</summary>
public class PulseMood : IPulsePhrase
{
    public Guid PulseId { get; set; }
    public required string Text { get; set; }
    public string? Emoji { get; set; }

    public Pulse Pulse { get; set; } = null!;
}

/// <summary>Need payload — the phrase the sender chose (e.g. "Could use a hug") + emoji.</summary>
public class PulseNeed : IPulsePhrase
{
    public Guid PulseId { get; set; }
    public required string Text { get; set; }
    public string? Emoji { get; set; }

    public Pulse Pulse { get; set; } = null!;
}

/// <summary>Thought payload — a short phrase (preset or custom) + emoji.</summary>
public class PulseThought : IPulsePhrase
{
    public Guid PulseId { get; set; }
    public required string Text { get; set; }
    public string? Emoji { get; set; }

    public Pulse Pulse { get; set; } = null!;
}

/// <summary>
/// PulseTouch payload — a hand-drawn doodle stored as vector strokes (JSON, in a jsonb column) so it
/// can be re-rendered later. Shape: { "version":1, "strokes":[{ "color":"#FF7A7A","width":4,
/// "points":[{"x":..,"y":..}] }] }. Coordinates are normalised 0–1 so the drawing scales to any canvas.
/// A preview image for the feed/widgets is a future addition; for now only the strokes are kept.
/// </summary>
public class PulseTouch
{
    public Guid PulseId { get; set; }

    /// <summary>Raw stroke JSON (stored as jsonb).</summary>
    public required string StrokeData { get; set; }

    public Pulse Pulse { get; set; } = null!;
}
