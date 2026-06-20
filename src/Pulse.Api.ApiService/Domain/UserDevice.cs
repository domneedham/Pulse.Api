namespace Pulse.Api.ApiService.Domain;

/// <summary>
/// A registered mobile device used for Firebase Cloud Messaging push notifications.
/// FcmToken is unique: re-registering an existing token moves it to the
/// authenticated user (tokens survive app reinstalls and account switches).
/// </summary>
public class UserDevice
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string FcmToken { get; set; }
    public DevicePlatform Platform { get; set; }
    public string? DeviceModel { get; set; }
    public string? DeviceName { get; set; }
    public string? OsVersion { get; set; }
    public string? AppVersion { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }

    public User User { get; set; } = null!;
}
