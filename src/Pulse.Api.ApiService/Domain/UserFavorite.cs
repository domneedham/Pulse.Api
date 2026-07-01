namespace Pulse.Api.ApiService.Domain;

/// <summary>
/// A phrase a user keeps as a favourite within a category (mood / need / thought). Favourites are the
/// quick options shown first in that category's send sheet; the user picks them during onboarding and
/// can add / remove / reorder them in settings. <see cref="SortOrder"/> drives display order.
/// </summary>
public class UserFavorite
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Which send category this favourite belongs to (Mood / Need / Thought).</summary>
    public PulseType Category { get; set; }

    /// <summary>The phrase, e.g. "Could use a hug". ≤ 80 chars.</summary>
    public required string Text { get; set; }

    /// <summary>Emoji shown beside the phrase; defaults to the category emoji when null.</summary>
    public string? Emoji { get; set; }

    public int SortOrder { get; set; }

    public User User { get; set; } = null!;
}
