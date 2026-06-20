namespace Pulse.Api.ApiService.Auth;

/// <summary>
/// Ensures the public "avatars" Storage bucket exists once on startup. Best-effort: a failure
/// (e.g. Supabase still booting) is logged but never blocks the app — the bucket can also be
/// created lazily on first upload if needed.
/// </summary>
public class AvatarBucketInitializer(
    ISupabaseStorageClient storage,
    ILogger<AvatarBucketInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await storage.EnsureAvatarsBucketAsync(cancellationToken);
            logger.LogInformation("Avatars storage bucket is ready.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not ensure the avatars bucket on startup; it'll be retried on first upload.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
