using Microsoft.EntityFrameworkCore;
using Pulse.Api.ApiService.Common;
using Pulse.Api.ApiService.Contracts;
using Pulse.Api.ApiService.Data;
using Pulse.Api.ApiService.Domain;

namespace Pulse.Api.ApiService.Services;

public interface IFavoritesService
{
    /// <summary>All of the user's favourites across categories, in order.</summary>
    Task<IReadOnlyList<FavoriteDto>> GetAllAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Replace the favourites for one category (onboarding picks / reorder). Order = sort order.</summary>
    Task<IReadOnlyList<FavoriteDto>> SetCategoryAsync(
        Guid userId, PulseType category, IReadOnlyList<FavoriteItem> items, CancellationToken ct = default);

    /// <summary>Append a single favourite (custom phrase or a catalogue option) to a category.</summary>
    Task<FavoriteDto> AddAsync(Guid userId, AddFavoriteRequest request, CancellationToken ct = default);

    /// <summary>Remove a favourite the user owns.</summary>
    Task DeleteAsync(Guid userId, Guid favoriteId, CancellationToken ct = default);

    /// <summary>The built-in catalogue of phrase options for a category ("more options" in the sheets).</summary>
    IReadOnlyList<FavoriteOptionDto> GetCatalog(PulseType category);

    /// <summary>
    /// Seed the default favourites for a user who has none yet (e.g. skipped onboarding). Idempotent:
    /// only seeds categories the user hasn't set.
    /// </summary>
    Task EnsureDefaultsAsync(Guid userId, CancellationToken ct = default);
}

public class FavoritesService(PulseDbContext db) : IFavoritesService
{
    private const int MaxPerCategory = 12;

    public async Task<IReadOnlyList<FavoriteDto>> GetAllAsync(Guid userId, CancellationToken ct = default)
    {
        var favorites = await db.UserFavorites
            .Where(f => f.UserId == userId)
            .OrderBy(f => f.Category).ThenBy(f => f.SortOrder)
            .ToListAsync(ct);

        return favorites.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<FavoriteDto>> SetCategoryAsync(
        Guid userId, PulseType category, IReadOnlyList<FavoriteItem> items, CancellationToken ct = default)
    {
        // Replace the whole category in one transaction: drop existing, insert in the given order.
        await db.UserFavorites
            .Where(f => f.UserId == userId && f.Category == category)
            .ExecuteDeleteAsync(ct);

        var clean = items
            .Select(i => i.Text.Trim())
            .Where(t => t.Length is > 0 and <= 80)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxPerCategory)
            .ToList();

        var order = 0;
        foreach (var item in items)
        {
            var text = item.Text.Trim();
            if (!clean.Contains(text, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            clean.Remove(text); // guard against the same phrase twice
            db.UserFavorites.Add(new UserFavorite
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                Category = category,
                Text = text,
                Emoji = string.IsNullOrWhiteSpace(item.Emoji)
                    ? FavoriteCatalog.DefaultEmoji(category)
                    : item.Emoji.Trim(),
                SortOrder = order++
            });
        }

        await db.SaveChangesAsync(ct);

        return (await db.UserFavorites
            .Where(f => f.UserId == userId && f.Category == category)
            .OrderBy(f => f.SortOrder).ToListAsync(ct))
            .Select(ToDto).ToList();
    }

    public async Task<FavoriteDto> AddAsync(Guid userId, AddFavoriteRequest request, CancellationToken ct = default)
    {
        var text = request.Text.Trim();
        if (text.Length is 0 or > 80)
        {
            throw new ValidationException("A favourite phrase must be 1–80 characters.");
        }

        var existing = await db.UserFavorites
            .Where(f => f.UserId == userId && f.Category == request.Category)
            .ToListAsync(ct);

        if (existing.Count >= MaxPerCategory)
        {
            throw new ConflictException($"You can keep up to {MaxPerCategory} favourites per category.");
        }

        if (existing.Any(f => string.Equals(f.Text, text, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ConflictException("That phrase is already a favourite.");
        }

        var favorite = new UserFavorite
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Category = request.Category,
            Text = text,
            Emoji = string.IsNullOrWhiteSpace(request.Emoji)
                ? FavoriteCatalog.DefaultEmoji(request.Category)
                : request.Emoji.Trim(),
            SortOrder = existing.Count == 0 ? 0 : existing.Max(f => f.SortOrder) + 1
        };

        db.UserFavorites.Add(favorite);
        await db.SaveChangesAsync(ct);

        return ToDto(favorite);
    }

    public async Task DeleteAsync(Guid userId, Guid favoriteId, CancellationToken ct = default)
    {
        var deleted = await db.UserFavorites
            .Where(f => f.Id == favoriteId && f.UserId == userId)
            .ExecuteDeleteAsync(ct);

        if (deleted == 0)
        {
            throw new NotFoundException("Favourite not found.");
        }
    }

    public IReadOnlyList<FavoriteOptionDto> GetCatalog(PulseType category) =>
        FavoriteCatalog.All(category).Select(o => new FavoriteOptionDto(o.Text, o.Emoji)).ToList();

    public async Task EnsureDefaultsAsync(Guid userId, CancellationToken ct = default)
    {
        var existingCategories = await db.UserFavorites
            .Where(f => f.UserId == userId)
            .Select(f => f.Category)
            .Distinct()
            .ToListAsync(ct);

        foreach (var category in new[] { PulseType.Mood, PulseType.Need, PulseType.Thought })
        {
            if (existingCategories.Contains(category))
            {
                continue;
            }

            var order = 0;
            foreach (var option in FavoriteCatalog.Defaults(category))
            {
                db.UserFavorites.Add(new UserFavorite
                {
                    Id = Guid.CreateVersion7(),
                    UserId = userId,
                    Category = category,
                    Text = option.Text,
                    Emoji = option.Emoji,
                    SortOrder = order++
                });
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static FavoriteDto ToDto(UserFavorite f) =>
        new(f.Id, f.Category, f.Text, f.Emoji ?? FavoriteCatalog.DefaultEmoji(f.Category), f.SortOrder);
}
