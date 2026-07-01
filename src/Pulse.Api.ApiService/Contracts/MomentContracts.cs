using Pulse.Api.ApiService.Domain;

namespace Pulse.Api.ApiService.Contracts;

// --- Submit responses (one shape per kind; photo is a separate multipart endpoint) ---

/// <summary>Submit a text answer to a Moment (reflection / love letter / fun pick / one word).</summary>
public record SubmitTextResponseRequest(string Text, string? Emoji);

/// <summary>Submit a drawing answer — vector stroke JSON, same format as PulseTouch.</summary>
public record SubmitDrawingResponseRequest(string StrokeData);

/// <summary>Submit a choice answer — the index of the picked option in the template's options.</summary>
public record SubmitChoiceResponseRequest(int ChoiceIndex);

// --- Read models ---

/// <summary>
/// One partner's response to a Moment. Payload fields are populated by <see cref="Kind"/>. Until the
/// caller has submitted their own response, the partner's response is withheld (reveal model) — only
/// <see cref="SubmittedByMe"/> + presence is exposed via the parent <see cref="MomentDto"/>.
/// </summary>
public record MomentResponseDto(
    Guid Id,
    MomentResponseKind Kind,
    bool SubmittedByMe,
    DateTimeOffset CreatedAt,
    string? Text = null,
    string? Emoji = null,
    string? StrokeData = null,
    string? PhotoUrl = null,
    string? VoiceUrl = null,
    int? ChoiceIndex = null);

/// <summary>
/// A Moment for the Trail / detail screen. <see cref="ResponseKind"/> tells the app which response sheet
/// to open. <see cref="MyResponseSubmitted"/> / <see cref="PartnerResponded"/> drive the progress UI;
/// <see cref="IsComplete"/> is the "completed together" reveal state. <see cref="Responses"/> is empty
/// until the caller has submitted theirs, then contains both (revealed).
/// </summary>
public record MomentDto(
    Guid Id,
    MomentCategory Category,
    string Title,
    string Prompt,
    MomentResponseKind ResponseKind,
    string Emoji,
    DateOnly Date,
    DateTimeOffset CreatedAt,
    bool MyResponseSubmitted,
    bool PartnerResponded,
    bool IsComplete,
    IReadOnlyList<MomentResponseDto> Responses,
    bool IsFavorite = false,
    IReadOnlyList<string>? Options = null);

/// <summary>Body for starring/unstarring a Moment (the Favorites tab on the Moments list).</summary>
public record SetMomentFavoriteRequest(bool IsFavorite);

/// <summary>
/// A unified Trail entry — either a pulse or a moment — so the app renders one merged, chronological
/// timeline. Exactly one of <see cref="Pulse"/> / <see cref="Moment"/> is set, matching <see cref="Kind"/>.
/// </summary>
public record TrailItemDto(
    TrailItemKind Kind,
    DateTimeOffset Timestamp,
    PulseDto? Pulse = null,
    MomentDto? Moment = null);

public enum TrailItemKind
{
    Pulse,
    Moment
}

/// <summary>
/// A pack in the store. <see cref="Unlocked"/> is true when the couple can use it (free, or a Pro member
/// is present). <see cref="Locked"/> is its inverse (drives the lock badge + upgrade prompt).
/// <see cref="Selected"/> is whether it's in the connection's daily pool. Core is always selected + unlocked.
/// </summary>
public record PackDto(
    Guid Id,
    string Key,
    string Title,
    string Emoji,
    bool IsPro,
    bool Unlocked,
    bool Locked,
    bool Selected,
    int TemplateCount);

/// <summary>Replace the connection's selected (Pro) pack ids. Core is implicit and need not be included.</summary>
public record SetConnectionPacksRequest(IReadOnlyList<Guid> PackIds);
