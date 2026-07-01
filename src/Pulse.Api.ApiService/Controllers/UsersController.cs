using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pulse.Api.ApiService.Auth;
using Pulse.Api.ApiService.Common;
using Pulse.Api.ApiService.Contracts;
using Pulse.Api.ApiService.Services;

namespace Pulse.Api.ApiService.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController(
    IUserService userService,
    ICurrentUser currentUser,
    IHostEnvironment environment) : ControllerBase
{
    /// <summary>The signed-in user's profile (the row is provisioned on first authenticated request).</summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetMe(CancellationToken ct) =>
        Ok(await userService.GetMeAsync(currentUser.Id, ct));

    [HttpPut("me")]
    public async Task<ActionResult<UserDto>> UpdateMe(UpdateProfileRequest request, CancellationToken ct) =>
        Ok(await userService.UpdateMeAsync(currentUser.Id, request, ct));

    /// <summary>GDPR account deletion — irreversible. Tombstones the profile and revokes sign-in.</summary>
    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMe(CancellationToken ct)
    {
        await userService.DeleteMeAsync(currentUser.Id, ct);
        return NoContent();
    }

    /// <summary>Upload a new avatar (multipart form field "file"). Returns the updated profile.</summary>
    [HttpPost("me/avatar")]
    public async Task<ActionResult<UserDto>> UploadAvatar(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            throw new ValidationException("No file provided.");
        }

        if (file.Length > 2 * 1024 * 1024)
        {
            throw new ValidationException("Avatar must be 2 MB or smaller.");
        }

        var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "image/png" : file.ContentType;
        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("Avatar must be an image.");
        }

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);

        return Ok(await userService.UploadAvatarAsync(currentUser.Id, ms.ToArray(), contentType, ct));
    }

    [HttpDelete("me/avatar")]
    public async Task<ActionResult<UserDto>> RemoveAvatar(CancellationToken ct) =>
        Ok(await userService.RemoveAvatarAsync(currentUser.Id, ct));

    /// <summary>Whether a username is available for the caller to claim.</summary>
    [HttpGet("username-available")]
    public async Task<ActionResult<UsernameAvailability>> CheckUsername(
        [FromQuery] string username, CancellationToken ct) =>
        Ok(await userService.CheckUsernameAsync(currentUser.Id, username, ct));

    /// <summary>
    /// DEV ONLY — toggle the caller's Pro flag without a payment provider. Returns 404 outside the
    /// Development environment so it can never be hit in production.
    /// </summary>
    [HttpPost("me/pro")]
    public async Task<ActionResult<UserDto>> SetPro(SetProRequest request, CancellationToken ct)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        return Ok(await userService.SetProAsync(currentUser.Id, request.IsPro, ct));
    }
}
