namespace Pulse.Api.ApiService.Auth;

/// <summary>
/// Ensures the public "moment-photos" Storage bucket exists once on startup. Best-effort, mirroring
/// <see cref="AvatarBucketInitializer"/>: a failure is logged but never blocks the app, and the bucket
/// is also created lazily on first upload.
/// </summary>
public class MomentPhotoBucketInitializer(
    ISupabaseStorageClient storage,
    ILogger<MomentPhotoBucketInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await storage.EnsureMomentPhotosBucketAsync(cancellationToken);
            logger.LogInformation("Moment photos storage bucket is ready.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not ensure the moment-photos bucket on startup; it'll be retried on first upload.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
