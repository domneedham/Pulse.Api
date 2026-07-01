using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pulse.Api.ApiService.Auth;
using Pulse.Api.ApiService.Contracts;
using Pulse.Api.ApiService.Domain;
using Pulse.Api.ApiService.Services;

namespace Pulse.Api.ApiService.Controllers;

[ApiController]
[Authorize]
[Route("api/favorites")]
public class FavoritesController(
    IFavoritesService favoritesService,
    ICurrentUser currentUser) : ControllerBase
{
    /// <summary>The caller's saved favourites across all categories, in order.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FavoriteDto>>> GetAll(CancellationToken ct) =>
        Ok(await favoritesService.GetAllAsync(currentUser.Id, ct));

    /// <summary>The built-in phrase catalogue for a category (onboarding / "more options").</summary>
    [HttpGet("catalog/{category}")]
    public ActionResult<IReadOnlyList<FavoriteOptionDto>> GetCatalog(PulseType category) =>
        Ok(favoritesService.GetCatalog(category));

    /// <summary>Replace the favourites for one category (onboarding picks or reorder).</summary>
    [HttpPut]
    public async Task<ActionResult<IReadOnlyList<FavoriteDto>>> SetCategory(
        SetFavoritesRequest request, CancellationToken ct) =>
        Ok(await favoritesService.SetCategoryAsync(currentUser.Id, request.Category, request.Items, ct));

    /// <summary>Add a single favourite to a category.</summary>
    [HttpPost]
    public async Task<ActionResult<FavoriteDto>> Add(AddFavoriteRequest request, CancellationToken ct) =>
        Ok(await favoritesService.AddAsync(currentUser.Id, request, ct));

    /// <summary>Remove a favourite.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await favoritesService.DeleteAsync(currentUser.Id, id, ct);
        return NoContent();
    }

    /// <summary>Seed default favourites for any category the user hasn't set (e.g. skipped onboarding).</summary>
    [HttpPost("defaults")]
    public async Task<ActionResult<IReadOnlyList<FavoriteDto>>> EnsureDefaults(CancellationToken ct)
    {
        await favoritesService.EnsureDefaultsAsync(currentUser.Id, ct);
        return Ok(await favoritesService.GetAllAsync(currentUser.Id, ct));
    }
}
