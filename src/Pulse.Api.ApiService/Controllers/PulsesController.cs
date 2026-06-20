using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pulse.Api.ApiService.Auth;
using Pulse.Api.ApiService.Contracts;
using Pulse.Api.ApiService.Services;

namespace Pulse.Api.ApiService.Controllers;

[ApiController]
[Authorize]
[Route("api/pulses")]
public class PulsesController(
    IPulseService pulseService,
    ICurrentUser currentUser) : ControllerBase
{
    /// <summary>The connection's timeline, newest first. <paramref name="before"/> pages older pulses.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PulseDto>>> GetTimeline(
        [FromQuery] DateTimeOffset? before, [FromQuery] int limit = 50, CancellationToken ct = default) =>
        Ok(await pulseService.GetTimelineAsync(currentUser.Id, before, limit, ct));

    /// <summary>The most recent pulse on the connection (Home "latest pulse"), or 204 if none yet.</summary>
    [HttpGet("latest")]
    public async Task<ActionResult<PulseDto>> GetLatest(CancellationToken ct)
    {
        var pulse = await pulseService.GetLatestAsync(currentUser.Id, ct);
        return pulse is null ? NoContent() : Ok(pulse);
    }

    [HttpPost("mood")]
    public async Task<ActionResult<PulseDto>> SendMood(SendMoodRequest request, CancellationToken ct) =>
        Ok(await pulseService.SendMoodAsync(currentUser.Id, request.MoodType, ct));

    [HttpPost("need")]
    public async Task<ActionResult<PulseDto>> SendNeed(SendNeedRequest request, CancellationToken ct) =>
        Ok(await pulseService.SendNeedAsync(currentUser.Id, request.NeedType, ct));

    [HttpPost("thought")]
    public async Task<ActionResult<PulseDto>> SendThought(SendThoughtRequest request, CancellationToken ct) =>
        Ok(await pulseService.SendThoughtAsync(currentUser.Id, request.Message, ct));
}
