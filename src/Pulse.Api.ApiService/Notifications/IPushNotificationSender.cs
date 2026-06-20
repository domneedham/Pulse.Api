namespace Pulse.Api.ApiService.Notifications;

/// <summary>Low-level push transport. Returns tokens the provider reports as dead.</summary>
public interface IPushNotificationSender
{
    Task<IReadOnlyList<string>> SendAsync(
        IReadOnlyList<string> fcmTokens,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default);
}
