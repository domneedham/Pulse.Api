using Microsoft.EntityFrameworkCore;
using Pulse.Api.ApiService.Auth;
using Pulse.Api.ApiService.Common;
using Pulse.Api.ApiService.Contracts;
using Pulse.Api.ApiService.Data;
using Pulse.Api.ApiService.Domain;

namespace Pulse.Api.ApiService.Services;

public interface IMomentService
{
    /// <summary>
    /// Today's Moment for the caller's connection (in the connection's timezone). Ensures today +
    /// tomorrow are scheduled if the background job hasn't yet. Both partners share the same Moment.
    /// </summary>
    Task<MomentDto> GetTodayAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Tomorrow's scheduled Moment — the Pro "peek ahead" (provisional; may change if packs change).
    /// Null if nothing is scheduled yet.
    /// </summary>
    Task<MomentDto?> GetUpcomingAsync(Guid userId, CancellationToken ct = default);

    /// <summary>A single Moment by id, on the caller's connection. 404 if not found.</summary>
    Task<MomentDto> GetByIdAsync(Guid userId, Guid momentId, CancellationToken ct = default);

    /// <summary>The connection's Moments (today + past), newest first. <paramref name="favoritesOnly"/> filters to starred.</summary>
    Task<IReadOnlyList<MomentDto>> GetMomentsAsync(Guid userId, bool favoritesOnly = false, CancellationToken ct = default);

    /// <summary>Star/unstar a Moment for the Favorites tab. Returns the updated Moment.</summary>
    Task<MomentDto> SetFavoriteAsync(Guid userId, Guid momentId, bool isFavorite, CancellationToken ct = default);

    /// <summary>Submit the caller's text answer. One response per partner.</summary>
    Task<MomentDto> SubmitTextAsync(Guid userId, Guid momentId, string text, string? emoji, CancellationToken ct = default);

    /// <summary>Submit the caller's drawing answer (vector stroke JSON).</summary>
    Task<MomentDto> SubmitDrawingAsync(Guid userId, Guid momentId, string strokeData, CancellationToken ct = default);

    /// <summary>Submit the caller's photo answer (already uploaded to Storage by the controller).</summary>
    Task<MomentDto> SubmitPhotoAsync(Guid userId, Guid momentId, string photoPath, string photoUrl, CancellationToken ct = default);

    /// <summary>Submit the caller's voice answer (already uploaded to Storage by the controller).</summary>
    Task<MomentDto> SubmitVoiceAsync(Guid userId, Guid momentId, string voicePath, string voiceUrl, CancellationToken ct = default);

    /// <summary>Submit the caller's choice answer — the picked option index (validated against the template).</summary>
    Task<MomentDto> SubmitChoiceAsync(Guid userId, Guid momentId, int choiceIndex, CancellationToken ct = default);

    /// <summary>The packs catalogue + the couple's selection/lock state for the store.</summary>
    Task<IReadOnlyList<PackDto>> GetPacksAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Replace the connection's selected (Pro) packs. Core is implicit. Selecting a Pro pack requires a
    /// Pro member (else 403). Recomputes the upcoming provisional Moment. Returns the refreshed catalogue.
    /// </summary>
    Task<IReadOnlyList<PackDto>> SetPacksAsync(Guid userId, IReadOnlyList<Guid> packIds, CancellationToken ct = default);

    /// <summary>
    /// Recompute the caller's provisional future Moments (i.e. tomorrow's peeked Moment) after their pack
    /// selection changes, so the upcoming Moment reflects the new packs. Call site for the (future)
    /// pack-selection endpoint; locked Moments (today/past or answered) are untouched.
    /// </summary>
    Task RecomputeUpcomingAsync(Guid userId, CancellationToken ct = default);
}

public class MomentService(PulseDbContext db, MomentAssignment assignment) : IMomentService
{
    public async Task<MomentDto> GetTodayAsync(Guid userId, CancellationToken ct = default)
    {
        var connection = await GetActiveConnectionAsync(userId, ct);
        var today = MomentAssignment.TodayFor(connection);

        // Ensure TODAY exists first (this is what the caller needs). Tomorrow's day-ahead row is a
        // best-effort extra — don't let scheduling it block returning today's Moment.
        await assignment.EnsureForDateAsync(connection.Id, today, ct);
        try
        {
            await assignment.EnsureForDateAsync(connection.Id, today.AddDays(1), ct);
        }
        catch
        {
            // ignore — tomorrow's peek can be created later by the job / GetUpcoming.
        }

        var moment = await LoadMomentQuery()
            .FirstOrDefaultAsync(m => m.ConnectionId == connection.Id && m.Date == today, ct)
            ?? throw new NotFoundException("No Moment is available yet.");

        return ToDto(moment, userId);
    }

    public async Task<MomentDto?> GetUpcomingAsync(Guid userId, CancellationToken ct = default)
    {
        var connection = await GetActiveConnectionAsync(userId, ct);
        var tomorrow = MomentAssignment.TodayFor(connection).AddDays(1);

        await assignment.EnsureScheduledAsync(connection, ct);

        var moment = await LoadMomentQuery()
            .FirstOrDefaultAsync(m => m.ConnectionId == connection.Id && m.Date == tomorrow, ct);

        return moment is null ? null : ToDto(moment, userId);
    }

    public async Task<MomentDto> GetByIdAsync(Guid userId, Guid momentId, CancellationToken ct = default)
    {
        var connection = await GetActiveConnectionAsync(userId, ct);
        var moment = await LoadMomentAsync(connection.Id, momentId, ct);
        return ToDto(moment, userId);
    }

    public async Task<IReadOnlyList<MomentDto>> GetMomentsAsync(
        Guid userId, bool favoritesOnly = false, CancellationToken ct = default)
    {
        var connection = await GetActiveConnectionAsync(userId, ct);
        var today = MomentAssignment.TodayFor(connection);

        // Today + past only (exclude tomorrow's provisional peek), newest first.
        var query = LoadMomentQuery()
            .Where(m => m.ConnectionId == connection.Id && m.Date <= today);
        if (favoritesOnly)
        {
            query = query.Where(m => m.IsFavorite);
        }

        var moments = await query
            .OrderByDescending(m => m.Date)
            .ToListAsync(ct);

        return moments.Select(m => ToDto(m, userId)).ToList();
    }

    public async Task<MomentDto> SetFavoriteAsync(
        Guid userId, Guid momentId, bool isFavorite, CancellationToken ct = default)
    {
        var connection = await GetActiveConnectionAsync(userId, ct);
        var moment = await LoadMomentAsync(connection.Id, momentId, ct);

        moment.IsFavorite = isFavorite;
        await db.SaveChangesAsync(ct);

        return ToDto(moment, userId);
    }

    public Task<MomentDto> SubmitTextAsync(Guid userId, Guid momentId, string text, string? emoji, CancellationToken ct = default) =>
        SubmitAsync(userId, momentId, MomentResponseKind.Text, (_, r) =>
        {
            var phrase = text.Trim();
            if (phrase.Length == 0)
            {
                throw new DomainRuleException("Your answer can't be empty.");
            }
            r.Text = phrase;
            r.Emoji = string.IsNullOrWhiteSpace(emoji) ? null : emoji.Trim();
        }, ct);

    public Task<MomentDto> SubmitDrawingAsync(Guid userId, Guid momentId, string strokeData, CancellationToken ct = default) =>
        SubmitAsync(userId, momentId, MomentResponseKind.Drawing, (_, r) =>
        {
            if (string.IsNullOrWhiteSpace(strokeData))
            {
                throw new DomainRuleException("A drawing needs at least one stroke.");
            }
            r.StrokeData = strokeData;
        }, ct);

    public Task<MomentDto> SubmitPhotoAsync(Guid userId, Guid momentId, string photoPath, string photoUrl, CancellationToken ct = default) =>
        SubmitAsync(userId, momentId, MomentResponseKind.Photo, (_, r) =>
        {
            r.PhotoPath = photoPath;
            r.PhotoUrl = photoUrl;
        }, ct);

    public Task<MomentDto> SubmitVoiceAsync(Guid userId, Guid momentId, string voicePath, string voiceUrl, CancellationToken ct = default) =>
        SubmitAsync(userId, momentId, MomentResponseKind.Voice, (_, r) =>
        {
            r.VoicePath = voicePath;
            r.VoiceUrl = voiceUrl;
        }, ct);

    public Task<MomentDto> SubmitChoiceAsync(Guid userId, Guid momentId, int choiceIndex, CancellationToken ct = default) =>
        SubmitAsync(userId, momentId, MomentResponseKind.Choice, (m, r) =>
        {
            var options = m.Template.Options;
            if (options is null || choiceIndex < 0 || choiceIndex >= options.Count)
            {
                throw new DomainRuleException("That isn't a valid choice for this Moment.");
            }
            r.ChoiceIndex = choiceIndex;
        }, ct);

    public async Task<IReadOnlyList<PackDto>> GetPacksAsync(Guid userId, CancellationToken ct = default)
    {
        var connection = await GetActiveConnectionAsync(userId, ct);
        var coupleIsPro = await CoupleIsProAsync(connection, ct);

        var selectedIds = await db.ConnectionPacks
            .Where(cp => cp.ConnectionId == connection.Id)
            .Select(cp => cp.PackId)
            .ToListAsync(ct);

        var packs = await db.Packs
            .OrderBy(p => p.SortOrder)
            .Select(p => new { p.Id, p.Key, p.Title, p.Emoji, p.IsPro, Count = p.Templates.Count })
            .ToListAsync(ct);

        return packs.Select(p =>
        {
            var unlocked = !p.IsPro || coupleIsPro;
            // Core is always part of the pool (implicit); Pro packs only when explicitly selected.
            var selected = p.Key == MomentCatalog.CorePackKey || selectedIds.Contains(p.Id);
            return new PackDto(p.Id, p.Key, p.Title, p.Emoji, p.IsPro, unlocked, !unlocked, selected, p.Count);
        }).ToList();
    }

    public async Task<IReadOnlyList<PackDto>> SetPacksAsync(
        Guid userId, IReadOnlyList<Guid> packIds, CancellationToken ct = default)
    {
        var connection = await GetActiveConnectionAsync(userId, ct);
        var coupleIsPro = await CoupleIsProAsync(connection, ct);

        // Resolve the requested packs (ignore Core / unknowns; Core is implicit and always eligible).
        var requested = await db.Packs
            .Where(p => packIds.Contains(p.Id) && p.Key != MomentCatalog.CorePackKey)
            .Select(p => new { p.Id, p.IsPro })
            .ToListAsync(ct);

        // Gate: a Pro pack can only be selected when the couple has a Pro member.
        if (requested.Any(p => p.IsPro) && !coupleIsPro)
        {
            throw new ForbiddenException("Pro packs need Pulse Pro. Upgrade to unlock them.");
        }

        var existing = await db.ConnectionPacks
            .Where(cp => cp.ConnectionId == connection.Id)
            .ToListAsync(ct);
        db.ConnectionPacks.RemoveRange(existing);

        foreach (var p in requested)
        {
            db.ConnectionPacks.Add(new ConnectionPack
            {
                ConnectionId = connection.Id,
                PackId = p.Id,
                AddedAt = DateTimeOffset.UtcNow
            });
        }

        await db.SaveChangesAsync(ct);

        // The pool changed — refresh tomorrow's provisional (peeked) Moment to match.
        await assignment.RecomputeFutureMomentsAsync(connection, ct);

        return await GetPacksAsync(userId, ct);
    }

    public async Task RecomputeUpcomingAsync(Guid userId, CancellationToken ct = default)
    {
        var connection = await GetActiveConnectionAsync(userId, ct);
        await assignment.RecomputeFutureMomentsAsync(connection, ct);
    }

    // --- submit ---

    private async Task<MomentDto> SubmitAsync(
        Guid userId, Guid momentId, MomentResponseKind kind, Action<Moment, MomentResponse> setPayload, CancellationToken ct)
    {
        var connection = await GetActiveConnectionAsync(userId, ct);
        var moment = await LoadMomentAsync(connection.Id, momentId, ct);

        if (moment.Template.ResponseKind != kind)
        {
            throw new DomainRuleException($"This Moment expects a {moment.Template.ResponseKind} answer.");
        }

        var existing = moment.Responses.FirstOrDefault(r => r.UserId == userId);
        if (existing is not null)
        {
            throw new ConflictException("You've already answered this Moment.");
        }

        var response = new MomentResponse
        {
            Id = Guid.CreateVersion7(),
            MomentId = moment.Id,
            UserId = userId,
            Kind = kind,
            CreatedAt = DateTimeOffset.UtcNow
        };
        setPayload(moment, response);

        db.MomentResponses.Add(response);
        await db.SaveChangesAsync(ct);

        moment.Responses.Add(response);
        return ToDto(moment, userId);
    }

    // --- helpers ---

    private IQueryable<Moment> LoadMomentQuery() =>
        db.Moments
            .Include(m => m.Template)
            .Include(m => m.Responses);

    private async Task<Moment> LoadMomentAsync(Guid connectionId, Guid momentId, CancellationToken ct) =>
        await LoadMomentQuery()
            .FirstOrDefaultAsync(m => m.Id == momentId && m.ConnectionId == connectionId, ct)
        ?? throw new NotFoundException("Moment not found.");

    private async Task<Connection> GetActiveConnectionAsync(Guid userId, CancellationToken ct)
    {
        var connection = await db.Connections
            .FirstOrDefaultAsync(c => c.Status == ConnectionStatus.Active
                && (c.UserAId == userId || c.UserBId == userId), ct);

        return connection
            ?? throw new DomainRuleException("You're not connected to anyone yet. Invite your partner first.");
    }

    /// <summary>True when either partner on the connection has Pro (Pro unlocks packs for both).</summary>
    private async Task<bool> CoupleIsProAsync(Connection connection, CancellationToken ct)
    {
        var memberIds = new[] { connection.UserAId, connection.UserBId }
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .ToArray();

        return await db.Users.AnyAsync(u => memberIds.Contains(u.Id) && u.IsPro, ct);
    }

    /// <summary>
    /// Maps a Moment to its DTO for <paramref name="userId"/>. Reveal model: a partner's response detail
    /// is only included once the caller has submitted their own — before that the caller sees that the
    /// partner responded (for the progress dots) but not the content.
    /// </summary>
    internal static MomentDto ToDto(Moment moment, Guid userId)
    {
        var mine = moment.Responses.FirstOrDefault(r => r.UserId == userId);
        var partner = moment.Responses.FirstOrDefault(r => r.UserId != userId);

        var myResponded = mine is not null;
        var partnerResponded = partner is not null;
        var revealed = myResponded && partnerResponded;

        // Withhold response content until both have answered (the "reveal together" rule). Before that,
        // expose only the caller's own response so their submitted state renders.
        var visible = new List<MomentResponse>();
        if (mine is not null)
        {
            visible.Add(mine);
        }
        if (revealed && partner is not null)
        {
            visible.Add(partner);
        }

        return new MomentDto(
            moment.Id,
            moment.Template.Category,
            moment.Template.Title,
            moment.Template.Prompt,
            moment.Template.ResponseKind,
            MomentCatalog.CategoryEmoji(moment.Template.Category),
            moment.Date,
            moment.CreatedAt,
            MyResponseSubmitted: myResponded,
            PartnerResponded: partnerResponded,
            IsComplete: revealed,
            Responses: visible
                .OrderBy(r => r.CreatedAt)
                .Select(r => new MomentResponseDto(
                    r.Id,
                    r.Kind,
                    SubmittedByMe: r.UserId == userId,
                    r.CreatedAt,
                    r.Text,
                    r.Emoji,
                    r.StrokeData,
                    r.PhotoUrl,
                    r.VoiceUrl,
                    r.ChoiceIndex))
                .ToList(),
            IsFavorite: moment.IsFavorite,
            Options: moment.Template.Options);
    }
}
