using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

namespace Pulse.Api.ApiService.Notifications;

public static class NotificationExtensions
{
    /// <summary>
    /// Registers push notification services. Firebase is enabled when either
    /// Firebase:CredentialsFile points at a service-account JSON or
    /// GOOGLE_APPLICATION_CREDENTIALS is set; otherwise a logging no-op is used
    /// so local development needs no Firebase project.
    /// </summary>
    public static IHostApplicationBuilder AddPulseNotifications(this IHostApplicationBuilder builder)
    {
        var credentialsFile = builder.Configuration["Firebase:CredentialsFile"];
        var hasAdc = !string.IsNullOrWhiteSpace(
            Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS"));

        if (!string.IsNullOrWhiteSpace(credentialsFile) || hasAdc)
        {
            if (FirebaseApp.DefaultInstance is null)
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = !string.IsNullOrWhiteSpace(credentialsFile)
                        ? CredentialFactory.FromFile<ServiceAccountCredential>(credentialsFile).ToGoogleCredential()
                        : GoogleCredential.GetApplicationDefault()
                });
            }

            builder.Services.AddSingleton<IPushNotificationSender, FcmPushNotificationSender>();
        }
        else
        {
            builder.Services.AddSingleton<IPushNotificationSender, NoOpPushNotificationSender>();
        }

        return builder;
    }
}
