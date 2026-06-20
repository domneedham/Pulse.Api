using System.Security.Claims;
using System.Text.Json;

namespace Pulse.Api.ApiService.Auth;

/// <summary>Identity of the caller, resolved from the validated Supabase JWT.</summary>
public interface ICurrentUser
{
    Guid Id { get; }
    string? Email { get; }

    /// <summary>Display name from Supabase user_metadata (set by Google/Apple/email sign-up).</summary>
    string? DisplayName { get; }

    string? AvatarUrl { get; }
}

public class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal Principal =>
        httpContextAccessor.HttpContext?.User
        ?? throw new InvalidOperationException("No active HTTP context.");

    public Guid Id =>
        Guid.TryParse(Principal.FindFirstValue("sub"), out var id)
            ? id
            : throw new InvalidOperationException("Authenticated principal has no valid 'sub' claim.");

    public string? Email => Principal.FindFirstValue("email");

    public string? DisplayName =>
        GetMetadataValue("full_name") ?? GetMetadataValue("name") ?? GetMetadataValue("display_name");

    public string? AvatarUrl => GetMetadataValue("avatar_url") ?? GetMetadataValue("picture");

    private string? GetMetadataValue(string key)
    {
        var metadataJson = Principal.FindFirstValue("user_metadata");
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(metadataJson);
            return doc.RootElement.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
