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

public class SupabaseStorageClient : ISupabaseStorageClient
{
    public const string Bucket = "avatars";

    private readonly HttpClient _httpClient;
    private readonly ILogger<SupabaseStorageClient> _logger;

    /// <summary>
    /// Base URL (ending in '/') used to build the public avatar URLs handed back to clients. This is
    /// deliberately separate from <see cref="HttpClient.BaseAddress"/>: the API talks to Supabase over
    /// the internal Docker network (e.g. http://supabase-kong:8000), but a name like that is
    /// unresolvable from a phone, so the URL we persist must be a LAN/public-reachable address.
    /// </summary>
    private readonly string _publicBaseUrl;

    public SupabaseStorageClient(
        HttpClient httpClient, IConfiguration configuration, ILogger<SupabaseStorageClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Prefer an explicit public URL; otherwise fall back to the internal base address so local
        // `aspire start` (where the gateway is reachable at localhost) keeps working unchanged.
        var publicUrl = configuration["Supabase:PublicUrl"]
            ?? configuration["ConnectionStrings:supabase:Url"]
            ?? httpClient.BaseAddress?.ToString()
            ?? throw new InvalidOperationException("No Supabase URL configured for building public avatar URLs.");

        _publicBaseUrl = publicUrl.TrimEnd('/') + "/";
    }

    public async Task EnsureAvatarsBucketAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "storage/v1/bucket",
            new { id = Bucket, name = Bucket, @public = true },
            cancellationToken);

        // 409 = already exists; that's the steady state, so treat it as success.
        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Conflict)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogWarning("Could not ensure avatars bucket ({Status}): {Body}", response.StatusCode, body);
        response.EnsureSuccessStatusCode();
    }

    public async Task<string> UploadAvatarAsync(
        string path, byte[] content, string contentType, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"storage/v1/object/{Bucket}/{path}");
        request.Headers.Add("x-upsert", "true"); // overwrite the user's previous avatar in place
        request.Content = new ByteArrayContent(content);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        // Public bucket → stable public URL, built from the client-reachable base (NOT the internal
        // Docker address used for the upload above). _publicBaseUrl ends with '/'.
        return $"{_publicBaseUrl}storage/v1/object/public/{Bucket}/{path}";
    }

    public async Task DeleteAvatarAsync(string path, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync($"storage/v1/object/{Bucket}/{path}", cancellationToken);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.OK)
        {
            return;
        }

        response.EnsureSuccessStatusCode();
    }
}
