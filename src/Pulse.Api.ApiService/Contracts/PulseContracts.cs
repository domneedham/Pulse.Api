using Pulse.Api.ApiService.Domain;

namespace Pulse.Api.ApiService.Contracts;

// --- Send requests (one per pulse type) ---

public record SendMoodRequest(MoodType MoodType);

public record SendNeedRequest(NeedType NeedType);

/// <summary>A thought: either a preset phrase or a custom message, max 50 chars.</summary>
public record SendThoughtRequest(string Message);

// --- Read models ---

/// <summary>
/// One pulse on the timeline. Exactly one of <see cref="MoodType"/> / <see cref="NeedType"/> /
/// <see cref="Message"/> is populated, matching <see cref="Type"/>. <see cref="SentByMe"/> lets the
/// app render direction without resolving sender ids.
/// </summary>
public record PulseDto(
    Guid Id,
    PulseType Type,
    bool SentByMe,
    DateTimeOffset CreatedAt,
    MoodType? MoodType = null,
    NeedType? NeedType = null,
    string? Message = null);
