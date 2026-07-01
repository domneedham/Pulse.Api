using Microsoft.EntityFrameworkCore;
using Pulse.Api.ApiService.Data;
using Pulse.Api.ApiService.Domain;

namespace Pulse.Api.ApiService.Services;

/// <summary>
/// Owns the rules for assigning a connection its daily Moment. Shared by the on-demand path
/// (<see cref="MomentService"/>) and the scheduled <see cref="MomentAssignmentJob"/> so there's one
/// definition of "which Moment, which day, which order".
///
/// Model:
/// - A connection gets one Moment per local day (its <see cref="Connection.Timezone"/> decides the day),
///   plus tomorrow is scheduled a day ahead so Pro users can peek.
/// - Templates are walked in a deterministic order: the connection's Nth assignment
///   (<see cref="Moment.SequenceNumber"/>) maps to <c>orderedPool[N % poolCount]</c>, looping when the
///   pool is exhausted. Stable + replayable, so history ("you missed your Nth Moment") is well-defined.
/// - A future, response-less Moment is PROVISIONAL: if the connection's pack selection changes it can be
///   recomputed/replaced. Once its day arrives or it has any response it's LOCKED.
/// - Eligible templates currently = the Core pack. The pool is computed from a SET of pack ids so adding
///   per-connection pack selection (connection_packs) later is a drop-in.
/// </summary>
public class MomentAssignment(PulseDbContext db)
{
    /// <summary>The local "today" for a connection, derived from its timezone (falls back to UTC).</summary>
    public static DateOnly TodayFor(Connection connection)
    {
        var tz = ResolveTimeZone(connection.Timezone);
        var localNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);
        return DateOnly.FromDateTime(localNow.DateTime);
    }

    private static TimeZoneInfo ResolveTimeZone(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return TimeZoneInfo.Utc;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }

    /// <summary>
    /// Ensures the connection has Moments for today and tomorrow (its local days), creating any missing
    /// ones. Idempotent — safe to call repeatedly (the unique (connection,date) index guards races).
    /// Returns the affected dates' Moments (with template + responses loaded).
    /// </summary>
    public async Task EnsureScheduledAsync(Connection connection, CancellationToken ct)
    {
        var today = TodayFor(connection);
        await EnsureForDateAsync(connection.Id, today, ct);
        await EnsureForDateAsync(connection.Id, today.AddDays(1), ct);
    }

    /// <summary>
    /// Creates the Moment for one date if it doesn't already exist. The template is chosen by the
    /// deterministic ordered walk at that date's sequence number. No-op if a row already exists.
    /// </summary>
    public async Task<Moment?> EnsureForDateAsync(Guid connectionId, DateOnly date, CancellationToken ct)
    {
        var existing = await db.Moments
            .FirstOrDefaultAsync(m => m.ConnectionId == connectionId && m.Date == date, ct);
        if (existing is not null)
        {
            return existing;
        }

        var (pool, usedOrder) = await LoadPoolAsync(connectionId, ct);
        if (pool.Count == 0)
        {
            return null;
        }

        // Sequence = how many days have been assigned strictly before this date (so backfilled/earlier
        // dates keep a stable, contiguous index regardless of insertion order).
        var sequence = usedOrder.Count(d => d < date);
        var pick = pool[sequence % pool.Count];

        var moment = new Moment
        {
            Id = Guid.CreateVersion7(),
            ConnectionId = connectionId,
            TemplateId = pick,
            Date = date,
            SequenceNumber = sequence,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Moments.Add(moment);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Raced with the partner / the job — use the row that won.
            db.Entry(moment).State = EntityState.Detached;
            return await db.Moments
                .FirstOrDefaultAsync(m => m.ConnectionId == connectionId && m.Date == date, ct);
        }

        return moment;
    }

    /// <summary>
    /// Recomputes PROVISIONAL future Moments (date &gt; the connection's local today, no responses): drops
    /// them and re-assigns from the connection's current eligible pool. Called when pack selection
    /// changes so an upcoming peeked Moment reflects the new packs. Locked Moments (today/past, or any
    /// with a response) are never touched.
    /// </summary>
    public async Task RecomputeFutureMomentsAsync(Connection connection, CancellationToken ct)
    {
        var today = TodayFor(connection);

        var future = await db.Moments
            .Where(m => m.ConnectionId == connection.Id && m.Date > today && m.Responses.Count == 0)
            .ToListAsync(ct);

        if (future.Count > 0)
        {
            db.Moments.RemoveRange(future);
            await db.SaveChangesAsync(ct);
        }

        // Re-create tomorrow from the current pool (today is already locked and untouched).
        await EnsureForDateAsync(connection.Id, today.AddDays(1), ct);
    }

    /// <summary>
    /// The ordered eligible template pool for a connection and the dates it has already been assigned.
    /// Pool is currently the Core pack ordered by (pack sort, template id) — deterministic. When
    /// per-connection pack selection lands, swap the pack-id source here; nothing else changes.
    /// </summary>
    private async Task<(IReadOnlyList<Guid> Pool, IReadOnlyList<DateOnly> UsedDates)> LoadPoolAsync(
        Guid connectionId, CancellationToken ct)
    {
        var packIds = await EligiblePackIdsAsync(connectionId, ct);

        var pool = await db.MomentTemplates
            .Where(t => packIds.Contains(t.PackId))
            .OrderBy(t => t.Pack.SortOrder)
            .ThenBy(t => t.Id)
            .Select(t => t.Id)
            .ToListAsync(ct);

        var usedDates = await db.Moments
            .Where(m => m.ConnectionId == connectionId)
            .Select(m => m.Date)
            .ToListAsync(ct);

        return (pool, usedDates);
    }

    /// <summary>
    /// The pack ids a connection draws from: the free Core pack (always) plus any packs the connection
    /// has selected (connection_packs). Pro-membership gating happens at selection time, so whatever is
    /// stored here is already authorised to be in the pool.
    /// </summary>
    private async Task<List<Guid>> EligiblePackIdsAsync(Guid connectionId, CancellationToken ct)
    {
        var coreId = await db.Packs
            .Where(p => p.Key == MomentCatalog.CorePackKey)
            .Select(p => p.Id)
            .FirstAsync(ct);

        var selected = await db.ConnectionPacks
            .Where(cp => cp.ConnectionId == connectionId)
            .Select(cp => cp.PackId)
            .ToListAsync(ct);

        selected.Add(coreId);
        return selected.Distinct().ToList();
    }
}
