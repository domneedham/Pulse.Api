using Microsoft.EntityFrameworkCore;
using Pulse.Api.ApiService.Common;
using Pulse.Api.ApiService.Contracts;
using Pulse.Api.ApiService.Data;
using Pulse.Api.ApiService.Domain;
using PulseEntity = Pulse.Api.ApiService.Domain.Pulse;

namespace Pulse.Api.ApiService.Services;

public interface IPulseService
{
    Task<PulseDto> SendMoodAsync(Guid userId, string text, string? emoji, string? note, CancellationToken ct = default);
    Task<PulseDto> SendNeedAsync(Guid userId, string text, string? emoji, string? note, CancellationToken ct = default);
    Task<PulseDto> SendThoughtAsync(Guid userId, string text, string? emoji, string? note, CancellationToken ct = default);

    /// <summary>Send a PulseTouch — a hand-drawn doodle stored as vector stroke JSON.</summary>
    Task<PulseDto> SendTouchAsync(Guid userId, string strokeData, CancellationToken ct = default);

    /// <summary>The connection's timeline, newest first, optionally paged with a <paramref name="before"/> cursor.</summary>
    Task<IReadOnlyList<PulseDto>> GetTimelineAsync(
        Guid userId, DateTimeOffset? before = null, int limit = 50, CancellationToken ct = default);

    /// <summary>
    /// The most recent pulse received FROM the partner (for the Home "latest from {partner}" card).
    /// Excludes the caller's own pulses. Null when the partner hasn't sent anything yet.
    /// </summary>
    Task<PulseDto?> GetLatestFromPartnerAsync(Guid userId, CancellationToken ct = default);

    /// <summary>A single pulse on the caller's connection, for the detail screen. 404 if not found.</summary>
    Task<PulseDto> GetByIdAsync(Guid userId, Guid pulseId, CancellationToken ct = default);

    /// <summary>The vector stroke JSON for a PulseTouch (the doodle viewer fetches this on open).</summary>
    Task<PulseTouchDto> GetTouchAsync(Guid userId, Guid pulseId, CancellationToken ct = default);

    /// <summary>The connection's favourited pulses, newest first.</summary>
    Task<IReadOnlyList<PulseDto>> GetFavoritesAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Search the connection's pulses by phrase text.</summary>
    Task<IReadOnlyList<PulseDto>> SearchAsync(Guid userId, string query, CancellationToken ct = default);

    /// <summary>Star/unstar a pulse for quick access. Returns the updated pulse.</summary>
    Task<PulseDto> SetFavoriteAsync(Guid userId, Guid pulseId, bool isFavorite, CancellationToken ct = default);

    /// <summary>React to a pulse with an emoji (null/empty clears it). Returns the updated pulse.</summary>
    Task<PulseDto> SetReactionAsync(Guid userId, Guid pulseId, string? emoji, CancellationToken ct = default);

    /// <summary>Delete a pulse. Only the sender may delete their own pulse; removes it for both partners.</summary>
    Task DeleteAsync(Guid userId, Guid pulseId, CancellationToken ct = default);
}

public class PulseService(PulseDbContext db) : IPulseService
{
    public Task<PulseDto> SendMoodAsync(Guid userId, string text, string? emoji, string? note, CancellationToken ct = default) =>
        SendAsync(userId, PulseType.Mood, text, emoji, note,
            (p, t, e) => p.Mood = new PulseMood { Text = t, Emoji = e }, ct);

    public Task<PulseDto> SendNeedAsync(Guid userId, string text, string? emoji, string? note, CancellationToken ct = default) =>
        SendAsync(userId, PulseType.Need, text, emoji, note,
            (p, t, e) => p.Need = new PulseNeed { Text = t, Emoji = e }, ct);

    public Task<PulseDto> SendThoughtAsync(Guid userId, string text, string? emoji, string? note, CancellationToken ct = default) =>
        SendAsync(userId, PulseType.Thought, text, emoji, note,
            (p, t, e) => p.Thought = new PulseThought { Text = t, Emoji = e }, ct);

    public async Task<PulseDto> SendTouchAsync(Guid userId, string strokeData, CancellationToken ct = default)
    {
        var connection = await GetActiveConnectionAsync(userId, ct);

        if (string.IsNullOrWhiteSpace(strokeData))
        {
            throw new DomainRuleException("A PulseTouch needs at least one stroke.");
        }

        var pulse = new PulseEntity
        {
            Id = Guid.CreateVersion7(),
            ConnectionId = connection.Id,
            SenderId = userId,
            Type = PulseType.Touch,
            CreatedAt = DateTimeOffset.UtcNow,
            Touch = new PulseTouch { StrokeData = strokeData }
        };

        db.Pulses.Add(pulse);
        await db.SaveChangesAsync(ct);

        return ToDto(pulse, userId);
    }

    private async Task<PulseDto> SendAsync(
        Guid userId, PulseType type, string text, string? emoji, string? note,
        Action<PulseEntity, string, string> setDetail, CancellationToken ct)
    {
        var connection = await GetActiveConnectionAsync(userId, ct);

        var phrase = text.Trim();
        if (phrase.Length == 0)
        {
            throw new DomainRuleException("A pulse needs a phrase.");
        }

        var resolvedEmoji = string.IsNullOrWhiteSpace(emoji) ? FavoriteCatalog.DefaultEmoji(type) : emoji.Trim();
        var trimmedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (trimmedNote is { Length: > 80 })
        {
            throw new DomainRuleException("A note can be up to 80 characters.");
        }

        var pulse = new PulseEntity
        {
            Id = Guid.CreateVersion7(),
            ConnectionId = connection.Id,
            SenderId = userId,
            Type = type,
            CreatedAt = DateTimeOffset.UtcNow,
            Note = trimmedNote
        };
        setDetail(pulse, phrase, resolvedEmoji);

        db.Pulses.Add(pulse);
        await db.SaveChangesAsync(ct);

        return ToDto(pulse, userId);
    }

    public async Task<IReadOnlyList<PulseDto>> GetTimelineAsync(
        Guid userId, DateTimeOffset? before = null, int limit = 50, CancellationToken ct = default)
    {
        var connection = await GetActiveConnectionAsync(userId, ct);

        var query = db.Pulses.Where(p => p.ConnectionId == connection.Id);
        if (before is not null)
        {
            query = query.Where(p => p.CreatedAt < before);
        }

        var pulses = await WithDetails(query)
            .OrderByDescending(p => p.CreatedAt)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(ct);

        return pulses.Select(p => ToDto(p, userId)).ToList();
    }

    public async Task<PulseDto?> GetLatestFromPartnerAsync(Guid userId, CancellationToken ct = default)
    {
        var connection = await GetActiveConnectionAsync(userId, ct);

        var pulse = await WithDetails(db.Pulses
                .Where(p => p.ConnectionId == connection.Id && p.SenderId != userId))
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(ct);

        return pulse is null ? null : ToDto(pulse, userId);
    }

    public async Task<PulseDto> GetByIdAsync(Guid userId, Guid pulseId, CancellationToken ct = default)
    {
        var connection = await GetActiveConnectionAsync(userId, ct);
        var pulse = await LoadPulseAsync(connection.Id, pulseId, ct);
        return ToDto(pulse, userId);
    }

    public async Task<PulseTouchDto> GetTouchAsync(Guid userId, Guid pulseId, CancellationToken ct = default)
    {
        var connection = await GetActiveConnectionAsync(userId, ct);

        var touch = await db.PulseTouches
            .Where(t => t.PulseId == pulseId && t.Pulse.ConnectionId == connection.Id)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("PulseTouch not found.");

        return new PulseTouchDto(touch.PulseId, touch.StrokeData);
    }

    public async Task<IReadOnlyList<PulseDto>> GetFavoritesAsync(Guid userId, CancellationToken ct = default)
    {
        var connection = await GetActiveConnectionAsync(userId, ct);

        var pulses = await WithDetails(db.Pulses
                .Where(p => p.ConnectionId == connection.Id && p.IsFavorite))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        return pulses.Select(p => ToDto(p, userId)).ToList();
    }

    public async Task<IReadOnlyList<PulseDto>> SearchAsync(Guid userId, string query, CancellationToken ct = default)
    {
        var connection = await GetActiveConnectionAsync(userId, ct);

        var term = query.Trim();
        if (term.Length == 0)
        {
            return [];
        }

        // All categories now carry phrase text, so match uniformly on the detail Text. History is small
        // (two people), so pull candidates and filter in memory for a case-insensitive contains.
        var pulses = await WithDetails(db.Pulses.Where(p => p.ConnectionId == connection.Id))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        var matches = pulses.Where(p =>
            p.Phrase?.Text.Contains(term, StringComparison.OrdinalIgnoreCase) == true);

        return matches.Select(p => ToDto(p, userId)).ToList();
    }

    public async Task<PulseDto> SetFavoriteAsync(
        Guid userId, Guid pulseId, bool isFavorite, CancellationToken ct = default)
    {
        var connection = await GetActiveConnectionAsync(userId, ct);
        var pulse = await LoadPulseAsync(connection.Id, pulseId, ct);

        pulse.IsFavorite = isFavorite;
        await db.SaveChangesAsync(ct);

        return ToDto(pulse, userId);
    }

    public async Task<PulseDto> SetReactionAsync(
        Guid userId, Guid pulseId, string? emoji, CancellationToken ct = default)
    {
        var connection = await GetActiveConnectionAsync(userId, ct);
        var pulse = await LoadPulseAsync(connection.Id, pulseId, ct);

        // Only the recipient reacts (you don't react to your own pulse).
        if (pulse.SenderId == userId)
        {
            throw new DomainRuleException("You can only react to pulses from your partner.");
        }

        pulse.Reaction = string.IsNullOrWhiteSpace(emoji) ? null : emoji.Trim();
        await db.SaveChangesAsync(ct);

        return ToDto(pulse, userId);
    }

    public async Task DeleteAsync(Guid userId, Guid pulseId, CancellationToken ct = default)
    {
        var connection = await GetActiveConnectionAsync(userId, ct);

        var pulse = await db.Pulses
            .FirstOrDefaultAsync(p => p.Id == pulseId && p.ConnectionId == connection.Id, ct)
            ?? throw new NotFoundException("Pulse not found.");

        // Only the sender can unsend; deleting removes it from the shared timeline for both partners.
        if (pulse.SenderId != userId)
        {
            throw new ForbiddenException("You can only delete pulses you sent.");
        }

        db.Pulses.Remove(pulse);
        await db.SaveChangesAsync(ct);
    }

    private static IQueryable<PulseEntity> WithDetails(IQueryable<PulseEntity> query) =>
        query.Include(p => p.Mood).Include(p => p.Need).Include(p => p.Thought).Include(p => p.Touch);

    private async Task<PulseEntity> LoadPulseAsync(Guid connectionId, Guid pulseId, CancellationToken ct) =>
        await WithDetails(db.Pulses.Where(p => p.Id == pulseId && p.ConnectionId == connectionId))
            .FirstOrDefaultAsync(ct)
        ?? throw new NotFoundException("Pulse not found.");

    /// <summary>Resolves the caller's active connection, or 422 if they aren't connected yet.</summary>
    private async Task<Connection> GetActiveConnectionAsync(Guid userId, CancellationToken ct)
    {
        var connection = await db.Connections
            .FirstOrDefaultAsync(c => c.Status == ConnectionStatus.Active
                && (c.UserAId == userId || c.UserBId == userId), ct);

        return connection
            ?? throw new DomainRuleException("You're not connected to anyone yet. Invite your partner first.");
    }

    private static PulseDto ToDto(PulseEntity pulse, Guid userId) => new(
        pulse.Id,
        pulse.Type,
        // Touch has no phrase (it's a drawing); show a label so it reads in the timeline. The drawing
        // itself is fetched separately by the viewer.
        Text: pulse.Type == PulseType.Touch ? "A doodle" : pulse.Phrase?.Text ?? string.Empty,
        Emoji: pulse.Phrase?.Emoji ?? FavoriteCatalog.DefaultEmoji(pulse.Type),
        SentByMe: pulse.SenderId == userId,
        pulse.CreatedAt,
        IsFavorite: pulse.IsFavorite,
        Reaction: pulse.Reaction,
        Note: pulse.Note,
        StrokeData: pulse.Touch?.StrokeData);
}
