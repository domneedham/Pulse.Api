using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pulse.Api.ApiService.Auth;
using Pulse.Api.ApiService.Common;
using Pulse.Api.ApiService.Contracts;
using Pulse.Api.ApiService.Domain;
using Pulse.Api.ApiService.Services;

namespace Pulse.Api.ApiService.Controllers;

[ApiController]
[Authorize]
[Route("api/moments")]
public class MomentsController(
    IMomentService momentService,
    ITrailService trailService,
    ISupabaseStorageClient storage,
    ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Today's shared Moment for the connection (assigned on first fetch of the day).</summary>
    [HttpGet("today")]
    public async Task<ActionResult<MomentDto>> GetToday(CancellationToken ct) =>
        Ok(await momentService.GetTodayAsync(currentUser.Id, ct));

    /// <summary>Tomorrow's scheduled Moment — the Pro "peek ahead". 204 if nothing's scheduled yet.</summary>
    [HttpGet("upcoming")]
    public async Task<ActionResult<MomentDto>> GetUpcoming(CancellationToken ct)
    {
        var moment = await momentService.GetUpcomingAsync(currentUser.Id, ct);
        return moment is null ? NoContent() : Ok(moment);
    }

    /// <summary>The merged Trail (pulses + moments), newest first. <paramref name="before"/> pages older items.</summary>
    [HttpGet("/api/trail")]
    public async Task<ActionResult<IReadOnlyList<TrailItemDto>>> GetTrail(
        [FromQuery] DateTimeOffset? before, [FromQuery] int limit = 50, CancellationToken ct = default) =>
        Ok(await trailService.GetTrailAsync(currentUser.Id, before, limit, ct));

    /// <summary>The packs catalogue with the couple's selection + lock state.</summary>
    [HttpGet("/api/packs")]
    public async Task<ActionResult<IReadOnlyList<PackDto>>> GetPacks(CancellationToken ct) =>
        Ok(await momentService.GetPacksAsync(currentUser.Id, ct));

    /// <summary>Replace the connection's selected (Pro) packs. 403 if a Pro pack is chosen without Pro.</summary>
    [HttpPut("/api/connection/packs")]
    public async Task<ActionResult<IReadOnlyList<PackDto>>> SetPacks(
        SetConnectionPacksRequest request, CancellationToken ct) =>
        Ok(await momentService.SetPacksAsync(currentUser.Id, request.PackIds ?? [], ct));

    /// <summary>The connection's Moments (today + past), newest first; <paramref name="favorites"/> filters to starred.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MomentDto>>> GetMoments(
        [FromQuery] bool favorites = false, CancellationToken ct = default) =>
        Ok(await momentService.GetMomentsAsync(currentUser.Id, favorites, ct));

    /// <summary>A single Moment, for the detail screen.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MomentDto>> GetById(Guid id, CancellationToken ct) =>
        Ok(await momentService.GetByIdAsync(currentUser.Id, id, ct));

    /// <summary>Star or unstar a Moment (Favorites tab).</summary>
    [HttpPut("{id:guid}/favorite")]
    public async Task<ActionResult<MomentDto>> SetFavorite(
        Guid id, SetMomentFavoriteRequest request, CancellationToken ct) =>
        Ok(await momentService.SetFavoriteAsync(currentUser.Id, id, request.IsFavorite, ct));

    [HttpPost("{id:guid}/respond/text")]
    public async Task<ActionResult<MomentDto>> RespondText(
        Guid id, SubmitTextResponseRequest request, CancellationToken ct) =>
        Ok(await momentService.SubmitTextAsync(currentUser.Id, id, request.Text, request.Emoji, ct));

    [HttpPost("{id:guid}/respond/drawing")]
    public async Task<ActionResult<MomentDto>> RespondDrawing(
        Guid id, SubmitDrawingResponseRequest request, CancellationToken ct) =>
        Ok(await momentService.SubmitDrawingAsync(currentUser.Id, id, request.StrokeData, ct));

    [HttpPost("{id:guid}/respond/choice")]
    public async Task<ActionResult<MomentDto>> RespondChoice(
        Guid id, SubmitChoiceResponseRequest request, CancellationToken ct) =>
        Ok(await momentService.SubmitChoiceAsync(currentUser.Id, id, request.ChoiceIndex, ct));

    /// <summary>Submit a voice answer (multipart form field "file"). Uploads to Storage, then records it.</summary>
    [HttpPost("{id:guid}/respond/voice")]
    public async Task<ActionResult<MomentDto>> RespondVoice(Guid id, IFormFile file, CancellationToken ct) =>
        await RespondMediaAsync(
            id, file, MomentResponseKind.Voice,
            maxBytes: 5 * 1024 * 1024, kindLabel: "voice note", expectedPrefix: "audio/",
            upload: (path, bytes, type) => storage.UploadMomentVoiceAsync(path, bytes, type, ct),
            submit: (path, url) => momentService.SubmitVoiceAsync(currentUser.Id, id, path, url, ct),
            defaultContentType: "audio/mp4", defaultExt: "m4a", ct: ct);

    /// <summary>Submit a photo answer (multipart form field "file"). Uploads to Storage, then records it.</summary>
    [HttpPost("{id:guid}/respond/photo")]
    public async Task<ActionResult<MomentDto>> RespondPhoto(Guid id, IFormFile file, CancellationToken ct) =>
        await RespondMediaAsync(
            id, file, MomentResponseKind.Photo,
            maxBytes: 3 * 1024 * 1024, kindLabel: "photo", expectedPrefix: "image/",
            upload: (path, bytes, type) => storage.UploadMomentPhotoAsync(path, bytes, type, ct),
            submit: (path, url) => momentService.SubmitPhotoAsync(currentUser.Id, id, path, url, ct),
            defaultContentType: "image/jpeg", defaultExt: "jpg", ct: ct);

    /// <summary>
    /// Shared multipart upload path for media responses (photo / voice): validates the file and the
    /// Moment BEFORE uploading (so a rejected submit never leaves an orphan object), uploads, then records.
    /// </summary>
    private async Task<ActionResult<MomentDto>> RespondMediaAsync(
        Guid id, IFormFile file, MomentResponseKind kind,
        long maxBytes, string kindLabel, string expectedPrefix,
        Func<string, byte[], string, Task<(string Path, string Url)>> upload,
        Func<string, string, Task<MomentDto>> submit,
        string defaultContentType, string defaultExt, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            throw new ValidationException("No file provided.");
        }

        if (file.Length > maxBytes)
        {
            throw new ValidationException($"{char.ToUpperInvariant(kindLabel[0])}{kindLabel[1..]} must be {maxBytes / (1024 * 1024)} MB or smaller.");
        }

        var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? defaultContentType : file.ContentType;
        if (!contentType.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException($"That file isn't a {kindLabel}.");
        }

        var moment = await momentService.GetByIdAsync(currentUser.Id, id, ct);
        if (moment.ResponseKind != kind)
        {
            throw new DomainRuleException($"This Moment doesn't expect a {kindLabel}.");
        }
        if (moment.MyResponseSubmitted)
        {
            throw new ConflictException("You've already answered this Moment.");
        }

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);

        var ext = contentType.Split('/').LastOrDefault() is { Length: > 0 } e ? e : defaultExt;
        var path = $"{id}/{currentUser.Id}.{ext}";
        var (storedPath, url) = await upload(path, ms.ToArray(), contentType);

        return Ok(await submit(storedPath, url));
    }
}
