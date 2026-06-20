namespace Pulse.Api.ApiService.Auth;

public class SupabaseAuthOptions
{
    public const string SectionName = "Supabase";

    /// <summary>HS256 secret GoTrue signs access tokens with. Injected by the AppHost locally.</summary>
    public string? JwtSecret { get; set; }

    /// <summary>
    /// Expected "iss" claim, e.g. "https://&lt;project-ref&gt;.supabase.co/auth/v1".
    /// Issuer validation is skipped when unset (the local stack's issuer varies with port mapping).
    /// </summary>
    public string? Issuer { get; set; }

    /// <summary>Supabase access tokens always carry the "authenticated" audience.</summary>
    public string Audience { get; set; } = "authenticated";
}
