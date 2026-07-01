namespace Pulse.Api.ApiService.Contracts;

/// <summary>The authenticated user's own profile.</summary>
public record UserDto(
    Guid Id,
    string DisplayName,
    string? AvatarUrl,
    string Timezone,
    DateTimeOffset CreatedAt,
    string? Username,
    bool IsPro = false);

/// <summary>Dev-only Pro toggle body (no payment provider yet).</summary>
public record SetProRequest(bool IsPro);

/// <summary>
/// Profile update. All fields optional-ish: DisplayName is required, the rest are applied only when
/// non-null so the client can patch one field at a time (e.g. just the username on profile setup).
/// </summary>
public record UpdateProfileRequest(
    string DisplayName,
    string? AvatarUrl,
    string? Timezone,
    string? Username);

/// <summary>Whether a username can be claimed, and why not when it can't.</summary>
public record UsernameAvailability(string Username, bool Available, string? Reason);
