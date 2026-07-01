using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Pulse.Api.ApiService.Common;
using Pulse.Api.ApiService.Contracts;
using Pulse.Api.ApiService.Data;
using Pulse.Api.ApiService.Domain;

namespace Pulse.Api.ApiService.Services;

public interface IConnectionService
{
    /// <summary>The caller's current connection (pending or active), or null if they have none.</summary>
    Task<ConnectionDto?> GetCurrentAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Creates a pending connection and returns it with a fresh invite code to share. Idempotent for
    /// a user already waiting on an invite — returns the existing pending connection rather than a
    /// second one. Fails if the user is already actively connected.
    /// </summary>
    Task<ConnectionDto> CreateInviteAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Joins a partner's pending connection using the code they shared; activates it.</summary>
    Task<ConnectionDto> AcceptInviteAsync(Guid userId, string inviteCode, CancellationToken ct = default);

    /// <summary>Cancels the caller's current connection (pending or active). Idempotent.</summary>
    Task CancelAsync(Guid userId, CancellationToken ct = default);
}

public class ConnectionService(PulseDbContext db) : IConnectionService
{
    public async Task<ConnectionDto?> GetCurrentAsync(Guid userId, CancellationToken ct = default)
    {
        var connection = await FindCurrentAsync(userId, ct);
        return connection is null ? null : await ToDtoAsync(connection, userId, ct);
    }

    public async Task<ConnectionDto> CreateInviteAsync(Guid userId, CancellationToken ct = default)
    {
        var existing = await FindCurrentAsync(userId, ct);
        if (existing is not null)
        {
            if (existing.IsActive)
            {
                throw new ConflictException("You're already connected. Disconnect first to invite someone new.");
            }

            // Already have an outstanding invite — hand back the same one (refresh the code if missing).
            existing.InviteCode ??= await GenerateUniqueCodeAsync(ct);
            await db.SaveChangesAsync(ct);
            return await ToDtoAsync(existing, userId, ct);
        }

        var connection = new Connection
        {
            Id = Guid.CreateVersion7(),
            UserAId = userId,
            Status = ConnectionStatus.Pending,
            InviteCode = await GenerateUniqueCodeAsync(ct),
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Connections.Add(connection);
        await db.SaveChangesAsync(ct);

        return await ToDtoAsync(connection, userId, ct);
    }

    public async Task<ConnectionDto> AcceptInviteAsync(Guid userId, string inviteCode, CancellationToken ct = default)
    {
        var code = inviteCode.Trim().ToUpperInvariant();

        var connection = await db.Connections
            .FirstOrDefaultAsync(c => c.InviteCode == code && c.Status == ConnectionStatus.Pending, ct)
            ?? throw new NotFoundException("That invite code isn't valid. Check it and try again.");

        if (connection.UserAId == userId)
        {
            throw new DomainRuleException("You can't accept your own invite.");
        }

        if (await FindCurrentAsync(userId, ct) is not null)
        {
            throw new ConflictException("You already have a connection. Disconnect first to join another.");
        }

        connection.UserBId = userId;
        connection.Status = ConnectionStatus.Active;
        connection.InviteCode = null;
        connection.ConnectedAt = DateTimeOffset.UtcNow;

        // Anchor the couple's "today" to the inviter's timezone (both partners then agree on the day).
        var inviterTz = await db.Users
            .Where(u => u.Id == connection.UserAId)
            .Select(u => u.Timezone)
            .FirstOrDefaultAsync(ct);
        if (!string.IsNullOrWhiteSpace(inviterTz))
        {
            connection.Timezone = inviterTz;
        }

        await db.SaveChangesAsync(ct);

        return await ToDtoAsync(connection, userId, ct);
    }

    public async Task CancelAsync(Guid userId, CancellationToken ct = default)
    {
        var connection = await FindCurrentAsync(userId, ct);
        if (connection is null)
        {
            return;
        }

        connection.Status = ConnectionStatus.Cancelled;
        connection.InviteCode = null;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>The user's one non-cancelled connection (pending or active), from either side.</summary>
    private Task<Connection?> FindCurrentAsync(Guid userId, CancellationToken ct) =>
        db.Connections
            .Where(c => c.Status != ConnectionStatus.Cancelled
                && (c.UserAId == userId || c.UserBId == userId))
            .FirstOrDefaultAsync(ct);

    private async Task<ConnectionDto> ToDtoAsync(Connection connection, Guid userId, CancellationToken ct)
    {
        PartnerDto? partner = null;
        var partnerId = connection.PartnerOf(userId);
        if (partnerId is not null)
        {
            partner = await db.Users
                .Where(u => u.Id == partnerId)
                .Select(u => new PartnerDto(u.Id, u.DisplayName, u.AvatarUrl, u.Username))
                .FirstOrDefaultAsync(ct);
        }

        // Only the inviter (and only while pending) should see the code to share.
        var code = connection.Status == ConnectionStatus.Pending && connection.UserAId == userId
            ? connection.InviteCode
            : null;

        return new ConnectionDto(
            connection.Id, connection.Status, code, partner, connection.CreatedAt, connection.ConnectedAt);
    }

    // Unambiguous, human-typeable codes: no 0/O/1/I, fixed length, retried on the rare collision.
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 6;

    private async Task<string> GenerateUniqueCodeAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var code = GenerateCode();
            if (!await db.Connections.AnyAsync(c => c.InviteCode == code, ct))
            {
                return code;
            }
        }

        throw new InvalidOperationException("Could not generate a unique invite code.");
    }

    private static string GenerateCode()
    {
        Span<char> chars = stackalloc char[CodeLength];
        for (var i = 0; i < CodeLength; i++)
        {
            chars[i] = CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)];
        }

        return new string(chars);
    }
}
