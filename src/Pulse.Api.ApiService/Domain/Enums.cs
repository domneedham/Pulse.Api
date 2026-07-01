namespace Pulse.Api.ApiService.Domain;

public enum DevicePlatform
{
    Ios,
    Android
}

/// <summary>The state of a connection between two users. A user has at most one non-cancelled connection.</summary>
public enum ConnectionStatus
{
    /// <summary>Invite issued; the other side hasn't accepted yet.</summary>
    Pending,
    Active,
    Cancelled
}

/// <summary>
/// The category of a pulse / favourite. A pulse carries free-text + an emoji within one of these
/// categories rather than a fixed enum value, so users can favourite their own phrases per category.
/// </summary>
public enum PulseType
{
    Mood = 1,
    Need = 2,
    Thought = 3,
    Touch = 4
}
