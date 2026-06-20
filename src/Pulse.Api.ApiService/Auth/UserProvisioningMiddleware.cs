using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Pulse.Api.ApiService.Data;
using Pulse.Api.ApiService.Domain;

namespace Pulse.Api.ApiService.Auth;

/// <summary>
/// Supabase Auth owns sign-up, so Pulse has no registration endpoint. Instead the
/// first authenticated request from a new auth user creates their profile row,
/// seeded from the JWT's user_metadata. A memory cache keeps this off the hot path.
/// Tombstoned (deleted) accounts are rejected here, since their JWT can remain
/// valid for up to an hour after deletion.
/// </summary>
public class UserProvisioningMiddleware(RequestDelegate next)
{
    private static readonly MemoryCacheEntryOptions CacheOptions =
        new() { SlidingExpiration = TimeSpan.FromHours(1) };

    public static string ProvisionedCacheKey(Guid userId) => $"user-provisioned:{userId}";
    public static string DeletedCacheKey(Guid userId) => $"user-deleted:{userId}";

    public async Task InvokeAsync(
        HttpContext context,
        PulseDbContext db,
        ICurrentUser currentUser,
        IMemoryCache cache,
        IProblemDetailsService problemDetailsService,
        ILogger<UserProvisioningMiddleware> logger)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = currentUser.Id;

            if (cache.TryGetValue(DeletedCacheKey(userId), out _))
            {
                await RejectDeletedAccountAsync(context, problemDetailsService);
                return;
            }

            if (!cache.TryGetValue(ProvisionedCacheKey(userId), out _))
            {
                var existing = await db.Users
                    .Where(u => u.Id == userId)
                    .Select(u => new { u.DeletedAt })
                    .FirstOrDefaultAsync(context.RequestAborted);

                if (existing is null)
                {
                    db.Users.Add(new User
                    {
                        Id = userId,
                        DisplayName = currentUser.DisplayName
                            ?? currentUser.Email?.Split('@')[0]
                            ?? "Pulse User",
                        AvatarUrl = currentUser.AvatarUrl,
                        CreatedAt = DateTimeOffset.UtcNow
                    });

                    try
                    {
                        await db.SaveChangesAsync(context.RequestAborted);
                        logger.LogInformation("Provisioned Pulse profile for new user {UserId}", userId);
                    }
                    catch (DbUpdateException)
                    {
                        // Concurrent first requests can race on the insert; the row exists either way.
                        db.ChangeTracker.Clear();
                    }
                }
                else if (existing.DeletedAt is not null)
                {
                    cache.Set(DeletedCacheKey(userId), true, CacheOptions);
                    await RejectDeletedAccountAsync(context, problemDetailsService);
                    return;
                }

                cache.Set(ProvisionedCacheKey(userId), true, CacheOptions);
            }
        }

        await next(context);
    }

    private static async Task RejectDeletedAccountAsync(
        HttpContext context, IProblemDetailsService problemDetailsService)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = "This account has been deleted."
            }
        });
    }
}
