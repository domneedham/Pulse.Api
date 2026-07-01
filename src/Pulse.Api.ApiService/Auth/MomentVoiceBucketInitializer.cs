namespace Pulse.Api.ApiService.Auth;

/// <summary>
/// Ensures the public "moment-voice" Storage bucket exists once on startup, mirroring
/// <see cref="MomentPhotoBucketInitializer"/>. Best-effort; the bucket is also created lazily on first upload.
/// </summary>
public class MomentVoiceBucketInitializer(
    ISupabaseStorageClient storage,
    ILogger<MomentVoiceBucketInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await storage.EnsureMomentVoiceBucketAsync(cancellationToken);
            logger.LogInformation("Moment voice storage bucket is ready.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not ensure the moment-voice bucket on startup; it'll be retried on first upload.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
