using Microsoft.EntityFrameworkCore;
using Pulse.Api.ApiService.Contracts;
using Pulse.Api.ApiService.Data;
using Pulse.Api.ApiService.Domain;

namespace Pulse.Api.ApiService.Services;

public interface IDeviceService
{
    /// <summary>
    /// Registers (or refreshes) an FCM device token. Tokens are upserted by value:
    /// if another account previously owned this token (account switch on the same
    /// phone), it is re-homed to the current user.
    /// </summary>
    Task<DeviceDto> RegisterAsync(Guid userId, RegisterDeviceRequest request, CancellationToken cancellationToken = default);

    /// <summary>Removes a token (sign-out / push opt-out). Idempotent.</summary>
    Task UnregisterAsync(Guid userId, string fcmToken, CancellationToken cancellationToken = default);
}

public class DeviceService(PulseDbContext db) : IDeviceService
{
    public async Task<DeviceDto> RegisterAsync(
        Guid userId, RegisterDeviceRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var device = await db.UserDevices
            .FirstOrDefaultAsync(d => d.FcmToken == request.FcmToken, cancellationToken);

        if (device is null)
        {
            device = new UserDevice
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                FcmToken = request.FcmToken,
                CreatedAt = now
            };
            db.UserDevices.Add(device);
        }

        device.UserId = userId;
        device.Platform = request.Platform;
        device.DeviceModel = request.DeviceModel;
        device.DeviceName = request.DeviceName;
        device.OsVersion = request.OsVersion;
        device.AppVersion = request.AppVersion;
        device.LastSeenAt = now;

        await db.SaveChangesAsync(cancellationToken);

        return new DeviceDto(
            device.Id, device.Platform, device.DeviceModel, device.DeviceName,
            device.OsVersion, device.AppVersion, device.LastSeenAt);
    }

    public async Task UnregisterAsync(
        Guid userId, string fcmToken, CancellationToken cancellationToken = default)
    {
        await db.UserDevices
            .Where(d => d.UserId == userId && d.FcmToken == fcmToken)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
