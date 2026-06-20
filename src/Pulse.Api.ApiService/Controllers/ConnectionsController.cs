using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pulse.Api.ApiService.Auth;
using Pulse.Api.ApiService.Contracts;
using Pulse.Api.ApiService.Services;

namespace Pulse.Api.ApiService.Controllers;

[ApiController]
[Authorize]
[Route("api/connection")]
public class ConnectionsController(
    IConnectionService connectionService,
    ICurrentUser currentUser) : ControllerBase
{
    /// <summary>The caller's current connection (pending or active), or 204 if they have none.</summary>
    [HttpGet]
    public async Task<ActionResult<ConnectionDto>> GetCurrent(CancellationToken ct)
    {
        var connection = await connectionService.GetCurrentAsync(currentUser.Id, ct);
        return connection is null ? NoContent() : Ok(connection);
    }

    /// <summary>Creates a pending connection and returns the invite code to share with a partner.</summary>
    [HttpPost("invite")]
    public async Task<ActionResult<ConnectionDto>> CreateInvite(CancellationToken ct) =>
        Ok(await connectionService.CreateInviteAsync(currentUser.Id, ct));

    /// <summary>Joins a partner's connection using the code they shared.</summary>
    [HttpPost("accept")]
    public async Task<ActionResult<ConnectionDto>> Accept(AcceptInviteRequest request, CancellationToken ct) =>
        Ok(await connectionService.AcceptInviteAsync(currentUser.Id, request.InviteCode, ct));

    /// <summary>Disconnects (cancels the current pending or active connection).</summary>
    [HttpDelete]
    public async Task<IActionResult> Cancel(CancellationToken ct)
    {
        await connectionService.CancelAsync(currentUser.Id, ct);
        return NoContent();
    }
}
