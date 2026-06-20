namespace Pulse.Api.ApiService.Notifications;

/// <summary>
/// Used when Firebase credentials are not configured (typical for local dev):
/// logs what would have been sent and keeps the rest of the pipeline identical.
/// </summary>
public class NoOpPushNotificationSender(ILogger<NoOpPushNotificationSender> logger) : IPushNotificationSender
{
    public Task<IReadOnlyList<string>> SendAsync(
        IReadOnlyList<string> fcmTokens,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Push (no-op, Firebase not configured) to {DeviceCount} device(s): {Title} — {Body}",
            fcmTokens.Count, title, body);

        return Task.FromResult<IReadOnlyList<string>>([]);
    }
}
