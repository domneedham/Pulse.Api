using Pulse.Api.ApiService.Domain;

namespace Pulse.Api.ApiService.Contracts;

// --- Send requests (one per category; they share a phrase + emoji shape but stay distinct so each
//     category can diverge later) ---

public record SendMoodRequest(string Text, string? Emoji, string? Note = null);
public record SendNeedRequest(string Text, string? Emoji, string? Note = null);
public record SendThoughtRequest(string Text, string? Emoji, string? Note = null);

/// <summary>Send a PulseTouch — vector stroke JSON for the hand-drawn doodle.</summary>
public record SendTouchRequest(string StrokeData);

/// <summary>Body for starring/unstarring a pulse (the Pulses-tab favourites view).</summary>
public record SetFavoriteRequest(bool IsFavorite);

/// <summary>Body for reacting to a pulse with an emoji (null/empty clears the reaction).</summary>
public record SetReactionRequest(string? Emoji);

/// <summary>
/// The type-specific detail for a PulseTouch — the raw vector stroke JSON. Fetched on demand when a
/// touch pulse is opened/rendered, keeping the lean <see cref="PulseDto"/> uniform across all types.
/// (The pattern generalises: other types can expose their own /{id}/{type} detail later.)
/// </summary>
public record PulseTouchDto(Guid Id, string StrokeData);

// --- Read model ---

/// <summary>
/// One pulse on the timeline. Text/Emoji are resolved from the matching detail row. <see cref="SentByMe"/>
/// lets the app render direction without resolving sender ids.
/// </summary>
public record PulseDto(
    Guid Id,
    PulseType Type,
    string Text,
    string Emoji,
    bool SentByMe,
    DateTimeOffset CreatedAt,
    bool IsFavorite = false,
    string? Reaction = null,
    string? Note = null);
