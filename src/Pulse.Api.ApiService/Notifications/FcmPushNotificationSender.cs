using FirebaseAdmin.Messaging;

namespace Pulse.Api.ApiService.Notifications;

/// <summary>Sends pushes through Firebase Cloud Messaging via the Admin SDK.</summary>
public class FcmPushNotificationSender(ILogger<FcmPushNotificationSender> logger) : IPushNotificationSender
{
    public async Task<IReadOnlyList<string>> SendAsync(
        IReadOnlyList<string> fcmTokens,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        if (fcmTokens.Count == 0)
        {
            return [];
        }

        var message = new MulticastMessage
        {
            Tokens = fcmTokens.ToList(),
            Notification = new Notification { Title = title, Body = body },
            Data = data?.ToDictionary(kv => kv.Key, kv => kv.Value)
        };

        var response = await FirebaseMessaging.DefaultInstance
            .SendEachForMulticastAsync(message, cancellationToken);

        var deadTokens = new List<string>();
        for (var i = 0; i < response.Responses.Count; i++)
        {
            var result = response.Responses[i];
            if (result.IsSuccess)
            {
                continue;
            }

            if (result.Exception?.MessagingErrorCode is MessagingErrorCode.Unregistered or MessagingErrorCode.InvalidArgument)
            {
                deadTokens.Add(fcmTokens[i]);
            }
            else
            {
                logger.LogWarning(result.Exception, "FCM send failed for one device");
            }
        }

        return deadTokens;
    }
}
