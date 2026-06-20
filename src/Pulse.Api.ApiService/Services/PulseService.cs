using Microsoft.EntityFrameworkCore;
using Pulse.Api.ApiService.Common;
using Pulse.Api.ApiService.Contracts;
using Pulse.Api.ApiService.Data;
using Pulse.Api.ApiService.Domain;
using PulseEntity = Pulse.Api.ApiService.Domain.Pulse;

namespace Pulse.Api.ApiService.Services;

public interface IPulseService
{
    Task<PulseDto> SendMoodAsync(Guid userId, MoodType moodType, CancellationToken ct = default);
    Task<PulseDto> SendNeedAsync(Guid userId, NeedType needType, CancellationToken ct = default);
    Task<PulseDto> SendThoughtAsync(Guid userId, string message, CancellationToken ct = default);

    /// <summary>The connection's timeline, newest first, optionally paged with a <paramref name="before"/> cursor.</summary>
    Task<IReadOnlyList<PulseDto>> GetTimelineAsync(
        Guid userId, DateTimeOffset? before = null, int limit = 50, CancellationToken ct = default);

    /// <summary>The most recent pulse on the connection (for the Home "latest pulse" card), or null.</summary>
    Task<PulseDto?> GetLatestAsync(Guid userId, CancellationToken ct = default);
}

public class PulseService(PulseDbContext db) : IPulseService
{
    public Task<PulseDto> SendMoodAsync(Guid userId, MoodType moodType, CancellationToken ct = default) =>
        SendAsync(userId, PulseType.Mood, p => p.Mood = new PulseMood { MoodType = moodType }, ct);

    public Task<PulseDto> SendNeedAsync(Guid userId, NeedType needType, CancellationToken ct = default) =>
        SendAsync(userId, PulseType.Need, p => p.Need = new PulseNeed { NeedType = needType }, ct);

    public Task<PulseDto> SendThoughtAsync(Guid userId, string message, CancellationToken ct = default) =>
        SendAsync(userId, PulseType.Thought, p => p.Thought = new PulseThought { Message = message.Trim() }, ct);

    private async Task<PulseDto> SendAsync(
        Guid userId, PulseType type, Action<PulseEntity> setDetail, CancellationToken ct)
    {
        var connection = await GetActiveConnectionAsync(userId, ct);

        var pulse = new PulseEntity
        {
            Id = Guid.CreateVersion7(),
            ConnectionId = connection.Id,
            SenderId = userId,
            Type = type,
            CreatedAt = DateTimeOffset.UtcNow
        };
        setDetail(pulse);

        db.Pulses.Add(pulse);
        await db.SaveChangesAsync(ct);

        return ToDto(pulse, userId);
    }

    public async Task<IReadOnlyList<PulseDto>> GetTimelineAsync(
        Guid userId, DateTimeOffset? before = null, int limit = 50, CancellationToken ct = default)
    {
        var connection = await GetActiveConnectionAsync(userId, ct);

        var query = db.Pulses
            .Where(p => p.ConnectionId == connection.Id);

        if (before is not null)
        {
            query = query.Where(p => p.CreatedAt < before);
        }

        var pulses = await query
            .OrderByDescending(p => p.CreatedAt)
            .Take(Math.Clamp(limit, 1, 100))
            .Include(p => p.Mood)
            .Include(p => p.Need)
            .Include(p => p.Thought)
            .ToListAsync(ct);

        return pulses.Select(p => ToDto(p, userId)).ToList();
    }

    public async Task<PulseDto?> GetLatestAsync(Guid userId, CancellationToken ct = default)
    {
        var connection = await GetActiveConnectionAsync(userId, ct);

        var pulse = await db.Pulses
            .Where(p => p.ConnectionId == connection.Id)
            .OrderByDescending(p => p.CreatedAt)
            .Include(p => p.Mood)
            .Include(p => p.Need)
            .Include(p => p.Thought)
            .FirstOrDefaultAsync(ct);

        return pulse is null ? null : ToDto(pulse, userId);
    }

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
        SentByMe: pulse.SenderId == userId,
        pulse.CreatedAt,
        MoodType: pulse.Mood?.MoodType,
        NeedType: pulse.Need?.NeedType,
        Message: pulse.Thought?.Message);
}
