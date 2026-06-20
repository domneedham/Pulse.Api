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

/// <summary>The kind of pulse. Each value has a matching detail table (mood/need/thought/touch).</summary>
public enum PulseType
{
    Mood = 1,
    Need = 2,
    Thought = 3,
    Touch = 4
}

/// <summary>Fixed mood set (no custom moods in MVP).</summary>
public enum MoodType
{
    Great = 1,
    Good = 2,
    Tired = 3,
    Stressed = 4,
    Loved = 5
}

/// <summary>Fixed need set (no custom needs in MVP).</summary>
public enum NeedType
{
    Hug = 1,
    Coffee = 2,
    Wine = 3,
    Listen = 4,
    QualityTime = 5,
    Space = 6
}
