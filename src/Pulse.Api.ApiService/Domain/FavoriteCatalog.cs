namespace Pulse.Api.ApiService.Domain;

/// <summary>A selectable phrase option (phrase + emoji) offered during onboarding / in the send sheets.</summary>
public record FavoriteOption(string Text, string Emoji);

/// <summary>
/// The built-in catalogue of phrase options per category, the default selection used when a user skips
/// onboarding, and the per-category fallback emoji. The full catalogue is what the send sheets show
/// under "More options"; the user's chosen favourites are the subset surfaced first.
/// </summary>
public static class FavoriteCatalog
{
    /// <summary>Emoji used when a custom phrase has none, keyed by category.</summary>
    public static string DefaultEmoji(PulseType category) => category switch
    {
        PulseType.Mood => "🙂",
        PulseType.Need => "💗",
        PulseType.Thought => "💬",
        PulseType.Touch => "✏️",
        _ => "💗"
    };

    public static IReadOnlyList<FavoriteOption> All(PulseType category) => category switch
    {
        PulseType.Mood => Moods,
        PulseType.Need => Needs,
        PulseType.Thought => Thoughts,
        _ => []
    };

    /// <summary>The default favourites chosen for a user who skips onboarding (first 5 of each).</summary>
    public static IReadOnlyList<FavoriteOption> Defaults(PulseType category) =>
        All(category).Take(5).ToList();

    private static readonly IReadOnlyList<FavoriteOption> Moods =
    [
        new("Great", "😄"),
        new("Tired", "😴"),
        new("Stressed", "😣"),
        new("Excited", "🎉"),
        new("Loved", "❤️"),
        new("Relaxed", "😌"),
        new("Anxious", "😟"),
        new("Productive", "💪"),
        new("Overwhelmed", "😵"),
        new("Lonely", "🥺"),
    ];

    private static readonly IReadOnlyList<FavoriteOption> Needs =
    [
        new("A hug", "🤗"),
        new("Coffee", "☕"),
        new("Some space", "🌙"),
        new("To talk", "💬"),
        new("Quality time", "🕰️"),
        new("A listening ear", "👂"),
        new("Encouragement", "✨"),
        new("Rest", "😴"),
        new("Help", "🙏"),
        new("Patience", "🍃"),
    ];

    private static readonly IReadOnlyList<FavoriteOption> Thoughts =
    [
        new("Thinking of you", "❤️"),
        new("Proud of you", "🌟"),
        new("You've got this", "💪"),
        new("Thank you", "🙏"),
        new("You made me smile", "😊"),
        new("Miss you", "🥰"),
        new("Good luck", "🍀"),
        new("Can't wait to see you", "🤍"),
        new("Sleep well", "🌙"),
        new("Have a great day", "☀️"),
    ];
}
