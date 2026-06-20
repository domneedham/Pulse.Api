using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pulse.Api.ApiService.Auth;
using Pulse.Api.ApiService.Contracts;
using Pulse.Api.ApiService.Services;

namespace Pulse.Api.ApiService.Controllers;

[ApiController]
[Authorize]
[Route("api/devices")]
public class DevicesController(
    IDeviceService deviceService,
    ICurrentUser currentUser) : ControllerBase
{
    /// <summary>
    /// Registers or refreshes this device's FCM token. The MAUI app should call this
    /// on every launch and whenever Firebase rotates the token.
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<DeviceDto>> Register(
        RegisterDeviceRequest request, CancellationToken cancellationToken) =>
        Ok(await deviceService.RegisterAsync(currentUser.Id, request, cancellationToken));

    /// <summary>Call on sign-out so the device stops receiving this account's pushes.</summary>
    [HttpDelete("{fcmToken}")]
    public async Task<IActionResult> Unregister(string fcmToken, CancellationToken cancellationToken)
    {
        await deviceService.UnregisterAsync(currentUser.Id, fcmToken, cancellationToken);
        return NoContent();
    }
}
