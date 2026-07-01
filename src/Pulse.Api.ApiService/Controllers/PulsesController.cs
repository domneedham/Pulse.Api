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

    /// <summary>The most recent pulse received from the partner (Home card), or 204 if none yet.</summary>
    [HttpGet("latest")]
    public async Task<ActionResult<PulseDto>> GetLatest(CancellationToken ct)
    {
        var pulse = await pulseService.GetLatestFromPartnerAsync(currentUser.Id, ct);
        return pulse is null ? NoContent() : Ok(pulse);
    }

    /// <summary>The connection's favourited pulses, newest first.</summary>
    [HttpGet("favorites")]
    public async Task<ActionResult<IReadOnlyList<PulseDto>>> GetFavorites(CancellationToken ct) =>
        Ok(await pulseService.GetFavoritesAsync(currentUser.Id, ct));

    /// <summary>Search the connection's pulses by text / mood / need label.</summary>
    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<PulseDto>>> Search(
        [FromQuery] string q, CancellationToken ct = default) =>
        Ok(await pulseService.SearchAsync(currentUser.Id, q ?? string.Empty, ct));

    /// <summary>A single pulse, for the detail screen.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PulseDto>> GetById(Guid id, CancellationToken ct) =>
        Ok(await pulseService.GetByIdAsync(currentUser.Id, id, ct));

    /// <summary>The vector stroke JSON for a PulseTouch, fetched by the doodle viewer on open.</summary>
    [HttpGet("{id:guid}/touch")]
    public async Task<ActionResult<PulseTouchDto>> GetTouch(Guid id, CancellationToken ct) =>
        Ok(await pulseService.GetTouchAsync(currentUser.Id, id, ct));

    [HttpPost("mood")]
    public async Task<ActionResult<PulseDto>> SendMood(SendMoodRequest request, CancellationToken ct) =>
        Ok(await pulseService.SendMoodAsync(currentUser.Id, request.Text, request.Emoji, request.Note, ct));

    [HttpPost("need")]
    public async Task<ActionResult<PulseDto>> SendNeed(SendNeedRequest request, CancellationToken ct) =>
        Ok(await pulseService.SendNeedAsync(currentUser.Id, request.Text, request.Emoji, request.Note, ct));

    [HttpPost("thought")]
    public async Task<ActionResult<PulseDto>> SendThought(SendThoughtRequest request, CancellationToken ct) =>
        Ok(await pulseService.SendThoughtAsync(currentUser.Id, request.Text, request.Emoji, request.Note, ct));

    [HttpPost("touch")]
    public async Task<ActionResult<PulseDto>> SendTouch(SendTouchRequest request, CancellationToken ct) =>
        Ok(await pulseService.SendTouchAsync(currentUser.Id, request.StrokeData, ct));

    /// <summary>Star or unstar a pulse.</summary>
    [HttpPut("{id:guid}/favorite")]
    public async Task<ActionResult<PulseDto>> SetFavorite(
        Guid id, SetFavoriteRequest request, CancellationToken ct) =>
        Ok(await pulseService.SetFavoriteAsync(currentUser.Id, id, request.IsFavorite, ct));

    /// <summary>React to a partner's pulse with an emoji (empty body clears it).</summary>
    [HttpPut("{id:guid}/reaction")]
    public async Task<ActionResult<PulseDto>> SetReaction(
        Guid id, SetReactionRequest request, CancellationToken ct) =>
        Ok(await pulseService.SetReactionAsync(currentUser.Id, id, request.Emoji, ct));

    /// <summary>Delete a pulse you sent (removes it from the shared timeline).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await pulseService.DeleteAsync(currentUser.Id, id, ct);
        return NoContent();
    }
}
