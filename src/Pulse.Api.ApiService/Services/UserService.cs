using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Pulse.Api.ApiService.Auth;
using Pulse.Api.ApiService.Common;
using Pulse.Api.ApiService.Contracts;
using Pulse.Api.ApiService.Data;
using Pulse.Api.ApiService.Domain;

namespace Pulse.Api.ApiService.Services;

public interface IUserService
{
    Task<UserDto> GetMeAsync(Guid userId, CancellationToken ct = default);
    Task<UserDto> UpdateMeAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default);
    Task<UserDto> UploadAvatarAsync(Guid userId, byte[] content, string contentType, CancellationToken ct = default);
    Task<UserDto> RemoveAvatarAsync(Guid userId, CancellationToken ct = default);
    Task<UsernameAvailability> CheckUsernameAsync(Guid userId, string username, CancellationToken ct = default);

    /// <summary>GDPR delete: tombstone the profile, drop the avatar, and revoke the auth user.</summary>
    Task DeleteMeAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Set the user's Pro flag. Dev-only (no payment provider yet); the controller gates this to Development.</summary>
    Task<UserDto> SetProAsync(Guid userId, bool isPro, CancellationToken ct = default);
}

public class UserService(
    PulseDbContext db,
    ISupabaseStorageClient storage,
    ISupabaseAdminClient admin,
    IMemoryCache cache) : IUserService
{
    public async Task<UserDto> GetMeAsync(Guid userId, CancellationToken ct = default) =>
        ToDto(await GetUserAsync(userId, ct));

    public async Task<UserDto> UpdateMeAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await GetUserAsync(userId, ct);

        user.DisplayName = request.DisplayName.Trim();

        if (request.AvatarUrl is not null)
        {
            user.AvatarUrl = request.AvatarUrl;
        }

        if (!string.IsNullOrWhiteSpace(request.Timezone))
        {
            user.Timezone = request.Timezone.Trim();
        }

        if (request.Username is not null)
        {
            var username = NormalizeUsername(request.Username);
            if (!string.Equals(username, user.Username, StringComparison.Ordinal)
                && await IsUsernameTakenAsync(username, userId, ct))
            {
                throw new ConflictException("That username is already taken.");
            }

            user.Username = username;
        }

        await db.SaveChangesAsync(ct);
        return ToDto(user);
    }

    public async Task<UserDto> UploadAvatarAsync(
        Guid userId, byte[] content, string contentType, CancellationToken ct = default)
    {
        var user = await GetUserAsync(userId, ct);

        // One object per user (upsert overwrites in place) so we never accumulate orphans.
        var url = await storage.UploadAvatarAsync($"{userId}.png", content, contentType, ct);
        user.AvatarUrl = url;

        await db.SaveChangesAsync(ct);
        return ToDto(user);
    }

    public async Task<UserDto> RemoveAvatarAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await GetUserAsync(userId, ct);

        await storage.DeleteAvatarAsync($"{userId}.png", ct);
        user.AvatarUrl = null;

        await db.SaveChangesAsync(ct);
        return ToDto(user);
    }

    public async Task<UsernameAvailability> CheckUsernameAsync(
        Guid userId, string username, CancellationToken ct = default)
    {
        var normalized = NormalizeUsername(username);

        if (!IsUsernameWellFormed(normalized))
        {
            return new UsernameAvailability(username, false, "3–30 letters, numbers or underscores.");
        }

        var taken = await IsUsernameTakenAsync(normalized, userId, ct);
        return new UsernameAvailability(username, !taken, taken ? "Already taken." : null);
    }

    public async Task DeleteMeAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await GetUserAsync(userId, ct);

        // Tombstone: wipe identifying fields but keep the row so a partner's connection/pulse history
        // stays intact. The user_devices cascade-delete; the avatar object is removed best-effort.
        await storage.DeleteAvatarAsync($"{userId}.png", ct);

        user.DisplayName = "Deleted user";
        user.AvatarUrl = null;
        user.Username = null;
        user.DeletedAt = DateTimeOffset.UtcNow;

        await db.UserDevices.Where(d => d.UserId == userId).ExecuteDeleteAsync(ct);
        await db.SaveChangesAsync(ct);

        // Revoke sign-in last (after our own state is consistent). Provisioning middleware caches the
        // deleted state, so flush so a stale "provisioned" entry can't resurrect access.
        await admin.DeleteAuthUserAsync(userId, ct);
        cache.Remove(UserProvisioningMiddleware.ProvisionedCacheKey(userId));
        cache.Set(UserProvisioningMiddleware.DeletedCacheKey(userId), true,
            new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromHours(1) });
    }

    public async Task<UserDto> SetProAsync(Guid userId, bool isPro, CancellationToken ct = default)
    {
        var user = await GetUserAsync(userId, ct);
        user.IsPro = isPro;
        await db.SaveChangesAsync(ct);
        return ToDto(user);
    }

    private async Task<User> GetUserAsync(Guid userId, CancellationToken ct) =>
        await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null, ct)
        ?? throw new NotFoundException("User not found.");

    private Task<bool> IsUsernameTakenAsync(string username, Guid excludingUserId, CancellationToken ct) =>
        db.Users.AnyAsync(u => u.Username == username && u.Id != excludingUserId, ct);

    // Usernames are stored lowercased so a plain unique index gives case-insensitive uniqueness.
    private static string NormalizeUsername(string username) => username.Trim().ToLowerInvariant();

    private static bool IsUsernameWellFormed(string username) =>
        username.Length is >= 3 and <= 30 && username.All(c => char.IsLetterOrDigit(c) || c == '_');

    private static UserDto ToDto(User u) =>
        new(u.Id, u.DisplayName, u.AvatarUrl, u.Timezone, u.CreatedAt, u.Username, u.IsPro);
}
