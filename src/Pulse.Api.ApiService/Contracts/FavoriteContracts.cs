using Pulse.Api.ApiService.Domain;

namespace Pulse.Api.ApiService.Contracts;

/// <summary>A saved favourite phrase in a category.</summary>
public record FavoriteDto(Guid Id, PulseType Category, string Text, string Emoji, int SortOrder);

/// <summary>A selectable phrase option from the built-in catalogue (onboarding / "more options").</summary>
public record FavoriteOptionDto(string Text, string Emoji);

/// <summary>Add a favourite to a category (custom phrase or one from the catalogue).</summary>
public record AddFavoriteRequest(PulseType Category, string Text, string? Emoji);

/// <summary>
/// Replace the whole favourites list for a category in one call (used by onboarding and reorder). The
/// order of <see cref="Items"/> becomes the sort order.
/// </summary>
public record SetFavoritesRequest(PulseType Category, IReadOnlyList<FavoriteItem> Items);

public record FavoriteItem(string Text, string? Emoji);
