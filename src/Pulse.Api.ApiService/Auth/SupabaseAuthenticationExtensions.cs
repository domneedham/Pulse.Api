using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Pulse.Api.ApiService.Auth;

public static class SupabaseAuthenticationExtensions
{
    /// <summary>
    /// Validates Supabase Auth (GoTrue) access tokens. Tokens are validated locally
    /// against the project's JWT secret, so no network round-trip to Supabase is needed.
    /// </summary>
    public static IHostApplicationBuilder AddSupabaseAuthentication(this IHostApplicationBuilder builder)
    {
        var options = builder.Configuration.GetSection(SupabaseAuthOptions.SectionName).Get<SupabaseAuthOptions>()
            ?? new SupabaseAuthOptions();

        if (string.IsNullOrWhiteSpace(options.JwtSecret))
        {
            throw new InvalidOperationException(
                $"Missing configuration '{SupabaseAuthOptions.SectionName}:JwtSecret'. " +
                "Locally this is injected by the AppHost; in production set it from the Supabase dashboard (Settings → API → JWT Secret).");
        }

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                // Keep Supabase claim names ("sub", "email", "role") instead of the
                // legacy Microsoft claim-type remapping.
                jwt.MapInboundClaims = false;

                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.JwtSecret)),
                    ValidateIssuerSigningKey = true,
                    ValidAudience = options.Audience,
                    ValidateAudience = true,
                    ValidIssuer = options.Issuer,
                    ValidateIssuer = !string.IsNullOrWhiteSpace(options.Issuer),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "sub",
                    RoleClaimType = "role"
                };
            });

        builder.Services.AddAuthorization();

        return builder;
    }
}
