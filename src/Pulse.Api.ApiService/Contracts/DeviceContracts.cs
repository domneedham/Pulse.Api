using Pulse.Api.ApiService.Domain;

namespace Pulse.Api.ApiService.Contracts;

public record RegisterDeviceRequest(
    string FcmToken,
    DevicePlatform Platform,
    string? DeviceModel,
    string? DeviceName,
    string? OsVersion,
    string? AppVersion);

public record DeviceDto(
    Guid Id,
    DevicePlatform Platform,
    string? DeviceModel,
    string? DeviceName,
    string? OsVersion,
    string? AppVersion,
    DateTimeOffset LastSeenAt);
