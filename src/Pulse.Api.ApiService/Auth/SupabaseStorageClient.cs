using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Pulse.Api.ApiService.Auth;

/// <summary>
/// Server-side access to Supabase Storage (service-role key) for the avatars bucket.
/// Objects are stored under a public bucket so <c>AvatarUrl</c> is a directly-loadable URL.
/// </summary>
public interface ISupabaseStorageClient
{
    /// <summary>Create the avatars bucket if it doesn't exist (public). Idempotent.</summary>
    Task EnsureAvatarsBucketAsync(CancellationToken cancellationToken = default);

    /// <summary>Upload (upsert) an avatar object and return its public URL.</summary>
    Task<string> UploadAvatarAsync(string path, byte[] content, string contentType, CancellationToken cancellationToken = default);

    /// <summary>Remove an avatar object. Idempotent (a missing object is success).</summary>
    Task DeleteAvatarAsync(string path, CancellationToken cancellationToken = default);
}

public class SupabaseStorageClient(HttpClient httpClient, ILogger<SupabaseStorageClient> logger) : ISupabaseStorageClient
{
    public const string Bucket = "avatars";

    public async Task EnsureAvatarsBucketAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "storage/v1/bucket",
            new { id = Bucket, name = Bucket, @public = true },
            cancellationToken);

        // 409 = already exists; that's the steady state, so treat it as success.
        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Conflict)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogWarning("Could not ensure avatars bucket ({Status}): {Body}", response.StatusCode, body);
        response.EnsureSuccessStatusCode();
    }

    public async Task<string> UploadAvatarAsync(
        string path, byte[] content, string contentType, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"storage/v1/object/{Bucket}/{path}");
        request.Headers.Add("x-upsert", "true"); // overwrite the user's previous avatar in place
        request.Content = new ByteArrayContent(content);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        // Public bucket → stable public URL. Base address ends with '/'.
        return $"{httpClient.BaseAddress}storage/v1/object/public/{Bucket}/{path}";
    }

    public async Task DeleteAvatarAsync(string path, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync($"storage/v1/object/{Bucket}/{path}", cancellationToken);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.OK)
        {
            return;
        }

        response.EnsureSuccessStatusCode();
    }
}
