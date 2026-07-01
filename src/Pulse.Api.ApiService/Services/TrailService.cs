using Microsoft.EntityFrameworkCore;
using Pulse.Api.ApiService.Common;
using Pulse.Api.ApiService.Contracts;
using Pulse.Api.ApiService.Data;
using Pulse.Api.ApiService.Domain;

namespace Pulse.Api.ApiService.Services;

public interface ITrailService
{
    /// <summary>
    /// The connection's merged Trail — pulses and moments interleaved newest-first. <paramref name="before"/>
    /// pages older items by timestamp.
    /// </summary>
    Task<IReadOnlyList<TrailItemDto>> GetTrailAsync(
        Guid userId, DateTimeOffset? before = null, int limit = 50, CancellationToken ct = default);
}

/// <summary>
/// Builds the unified Trail by drawing both pulses (via <see cref="IPulseService"/>) and moments and
/// merging them on timestamp. History is small (two people), so a per-page fetch-and-merge is fine.
/// </summary>
public class TrailService(PulseDbContext db, IPulseService pulses) : ITrailService
{
    public async Task<IReadOnlyList<TrailItemDto>> GetTrailAsync(
        Guid userId, DateTimeOffset? before = null, int limit = 50, CancellationToken ct = default)
    {
        var connection = await GetActiveConnectionAsync(userId, ct);
        var take = Math.Clamp(limit, 1, 100);

        // Pull a page of each side, merge, and trim — fetching `take` of each guarantees the merged top
        // `take` is correct regardless of how they interleave.
        var pulseItems = (await pulses.GetTimelineAsync(userId, before, take, ct))
            .Select(p => new TrailItemDto(TrailItemKind.Pulse, p.CreatedAt, Pulse: p));

        // The Trail is the couple's *history*: today and past Moments only. Tomorrow's provisional
        // (day-ahead) Moment is the Pro "peek", not part of the shared timeline, so exclude future dates.
        var today = MomentAssignment.TodayFor(connection);

        var momentQuery = db.Moments
            .Where(m => m.ConnectionId == connection.Id && m.Date <= today);
        if (before is not null)
        {
            momentQuery = momentQuery.Where(m => m.CreatedAt < before);
        }

        var moments = await momentQuery
            .Include(m => m.Template)
            .Include(m => m.Responses)
            .OrderByDescending(m => m.Date)
            .Take(take)
            .ToListAsync(ct);

        // Anchor a Moment to noon on its local Date so it day-groups correctly and sorts sensibly against
        // pulses (rather than using CreatedAt, which can differ for scheduled/backfilled rows).
        var momentItems = moments
            .Select(m => new TrailItemDto(
                TrailItemKind.Moment,
                new DateTimeOffset(m.Date.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero),
                Moment: MomentService.ToDto(m, userId)));

        return pulseItems
            .Concat(momentItems)
            .OrderByDescending(i => i.Timestamp)
            .Take(take)
            .ToList();
    }

    private async Task<Connection> GetActiveConnectionAsync(Guid userId, CancellationToken ct)
    {
        var connection = await db.Connections
            .FirstOrDefaultAsync(c => c.Status == ConnectionStatus.Active
                && (c.UserAId == userId || c.UserBId == userId), ct);

        return connection
            ?? throw new DomainRuleException("You're not connected to anyone yet. Invite your partner first.");
    }
}
