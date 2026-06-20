using System.Net;

namespace Pulse.Api.ApiService.Auth;

/// <summary>Server-side calls to the Supabase Auth admin API (service-role key).</summary>
public interface ISupabaseAdminClient
{
    /// <summary>
    /// Permanently deletes the auth user (GoTrue), revoking their ability to sign in.
    /// Idempotent: an already-deleted user is treated as success.
    /// </summary>
    Task DeleteAuthUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

public class SupabaseAdminClient(HttpClient httpClient) : ISupabaseAdminClient
{
    public async Task DeleteAuthUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync($"auth/v1/admin/users/{userId}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        response.EnsureSuccessStatusCode();
    }
}
