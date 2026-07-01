namespace Pulse.Api.ApiService.Domain;

/// <summary>
/// The broad flavour of a Moment, mirroring the brief's categories. Drives the icon/colour shown on the
/// Trail card and groups templates within a pack. Stored as a string.
/// </summary>
public enum MomentCategory
{
    Capture,
    Draw,
    LoveLetter,
    Voice,
    Fun,
    Adventure,
    Reflection,
    Puzzle,
    Micro
}

/// <summary>
/// How a partner answers a Moment. Determines which response sheet the app shows and which payload column
/// on <see cref="MomentResponse"/> is populated. Voice is modelled but not wired end-to-end yet.
/// </summary>
public enum MomentResponseKind
{
    /// <summary>A short written answer (reflection, love letter, fun pick, one word).</summary>
    Text,

    /// <summary>A hand-drawn doodle — reuses the PulseTouch vector-stroke format (jsonb).</summary>
    Drawing,

    /// <summary>A captured photo, uploaded to the moment-photos Storage bucket.</summary>
    Photo,

    /// <summary>A short voice note, uploaded to the moment-voice Storage bucket.</summary>
    Voice,

    /// <summary>A pick from the template's fixed options (this-or-that / would you rather).</summary>
    Choice
}

/// <summary>
/// A themed collection of <see cref="MomentTemplate"/>s. The free <c>core</c> pack is the source of the
/// daily Moment everyone gets; <see cref="IsPro"/> packs are unlocked experiences (Photography, Adventure,
/// Romance, Garden, …) surfaced in a future store. Purchases aren't modelled yet — Pro packs are listed
/// but locked.
/// </summary>
public class Pack
{
    public Guid Id { get; set; }

    /// <summary>Stable slug used in code/seed data (e.g. "core", "photography").</summary>
    public required string Key { get; set; }

    public required string Title { get; set; }
    public required string Emoji { get; set; }

    /// <summary>True for paid packs; the free daily Moment is only ever drawn from non-Pro packs.</summary>
    public bool IsPro { get; set; }

    public int SortOrder { get; set; }

    public ICollection<MomentTemplate> Templates { get; set; } = [];
}

/// <summary>
/// A pack a connection has added to its daily Moment pool. The free Core pack is always eligible and is
/// NOT stored here — only the (Pro) packs a couple has opted into. Selecting a Pro pack requires a Pro
/// member on the connection. Composite key (ConnectionId, PackId).
/// </summary>
public class ConnectionPack
{
    public Guid ConnectionId { get; set; }
    public Guid PackId { get; set; }
    public DateTimeOffset AddedAt { get; set; }

    public Connection Connection { get; set; } = null!;
    public Pack Pack { get; set; } = null!;
}

/// <summary>
/// The blueprint for a Moment — a reusable prompt within a pack. A <see cref="Moment"/> is one instance of
/// a template assigned to a connection on a given day. Templates are seeded in code (no admin UI).
/// </summary>
public class MomentTemplate
{
    public Guid Id { get; set; }
    public Guid PackId { get; set; }

    public MomentCategory Category { get; set; }

    /// <summary>Short title shown on the card, e.g. "Sunset walk".</summary>
    public required string Title { get; set; }

    /// <summary>The instruction, e.g. "Photograph something that made you smile today."</summary>
    public required string Prompt { get; set; }

    /// <summary>How both partners answer it.</summary>
    public MomentResponseKind ResponseKind { get; set; }

    /// <summary>
    /// For <see cref="MomentResponseKind.Choice"/> templates: the fixed options each partner picks from
    /// (e.g. ["Mountains","Beach","City"]), stored as a JSON array (jsonb). Null/empty for other kinds.
    /// A <see cref="MomentResponse.ChoiceIndex"/> indexes into this list.
    /// </summary>
    public IReadOnlyList<string>? Options { get; set; }

    public Pack Pack { get; set; } = null!;
}

/// <summary>
/// One Moment assigned to a connection for a specific day. Both partners answer the same template; the
/// Moment is "completed together" once each has submitted a <see cref="MomentResponse"/>. At most one
/// Moment per connection per day (unique on <see cref="ConnectionId"/> + <see cref="Date"/>).
/// </summary>
public class Moment
{
    public Guid Id { get; set; }
    public Guid ConnectionId { get; set; }
    public Guid TemplateId { get; set; }

    /// <summary>The local calendar day this Moment is for (in the connection's timezone, date-only).</summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// The couple's 0-based assignment index — their Nth Moment ever. Drives the deterministic ordered
    /// walk through the eligible templates (template = orderedPool[SequenceNumber % poolCount]), so the
    /// progression is stable/replayable and "you missed your Nth Moment" is well-defined for history.
    /// </summary>
    public int SequenceNumber { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Whether a member has starred this Moment for the Favorites tab on the Moments list.</summary>
    public bool IsFavorite { get; set; }

    public Connection Connection { get; set; } = null!;
    public MomentTemplate Template { get; set; } = null!;

    /// <summary>The (0–2) responses submitted so far, at most one per partner.</summary>
    public ICollection<MomentResponse> Responses { get; set; } = [];

    /// <summary>True once both partners have responded — the reveal/"completed together" state.</summary>
    public bool IsComplete => Responses.Count >= 2;
}

/// <summary>
/// One partner's answer to a Moment. Exactly one payload is populated, matching <see cref="Kind"/>:
/// <see cref="Text"/>(+<see cref="Emoji"/>) for Text, <see cref="StrokeData"/> for Drawing, or
/// <see cref="PhotoPath"/> for Photo. Unique per (Moment, User) — a partner answers once.
/// </summary>
public class MomentResponse
{
    public Guid Id { get; set; }
    public Guid MomentId { get; set; }

    /// <summary>The partner who submitted this response.</summary>
    public Guid UserId { get; set; }

    public MomentResponseKind Kind { get; set; }

    /// <summary>Text answer (Text kind).</summary>
    public string? Text { get; set; }

    /// <summary>Emoji accompanying a text answer (Text kind).</summary>
    public string? Emoji { get; set; }

    /// <summary>Vector stroke JSON, same shape as PulseTouch (Drawing kind, jsonb).</summary>
    public string? StrokeData { get; set; }

    /// <summary>Storage path of the uploaded photo in the moment-photos bucket (Photo kind).</summary>
    public string? PhotoPath { get; set; }

    /// <summary>Public URL to the uploaded photo (Photo kind).</summary>
    public string? PhotoUrl { get; set; }

    /// <summary>Storage path of the uploaded voice note in the moment-voice bucket (Voice kind).</summary>
    public string? VoicePath { get; set; }

    /// <summary>Public URL to the uploaded voice note (Voice kind).</summary>
    public string? VoiceUrl { get; set; }

    /// <summary>Index into the template's <see cref="MomentTemplate.Options"/> for the chosen option (Choice kind).</summary>
    public int? ChoiceIndex { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Moment Moment { get; set; } = null!;
    public User User { get; set; } = null!;
}
