using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Pulse.Api.ApiService.Auth;
using Pulse.Api.ApiService.Common;
using Pulse.Api.ApiService.Data;
using Pulse.Api.ApiService.Notifications;
using Pulse.Api.ApiService.Services;
using Pulse.Api.ApiService.Validation;

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults: OpenTelemetry, health checks, service discovery, resilience.
builder.AddServiceDefaults();

// EF Core against the Supabase Postgres (connection injected by the AppHost),
// with Aspire-managed health checks, tracing, and connection retries.
builder.AddNpgsqlDbContext<PulseDbContext>(
    "pulsedb",
    configureDbContextOptions: options => options.UseSnakeCaseNamingConvention());

builder.AddSupabaseAuthentication();
builder.AddPulseNotifications();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();

builder.Services
    .AddControllers(options =>
    {
        options.Filters.Add<ValidationFilter>();
        // Endpoints with an optional request body (e.g. start) accept an empty one.
        options.AllowEmptyInputInBodyModelBinding = true;
    })
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// GoTrue admin API access for GDPR account deletion. The configure callback runs
// lazily on first use, so missing config only fails the delete endpoint, not boot.
static void ConfigureSupabaseAdminHttpClient(WebApplicationBuilder builder, HttpClient client)
{
    var url = builder.Configuration["ConnectionStrings:supabase:Url"]
        ?? throw new InvalidOperationException(
            "Missing 'ConnectionStrings:supabase:Url' (Supabase API gateway). Injected by the AppHost locally; set it to https://<project-ref>.supabase.co in production.");
    var serviceRoleKey = builder.Configuration["Supabase:ServiceRoleKey"]
        ?? throw new InvalidOperationException(
            "Missing 'Supabase:ServiceRoleKey'. Injected by the AppHost locally; set it from the Supabase dashboard (Settings → API) in production.");

    client.BaseAddress = new Uri(url.TrimEnd('/') + "/");
    client.DefaultRequestHeaders.Add("apikey", serviceRoleKey);
    client.DefaultRequestHeaders.Authorization = new("Bearer", serviceRoleKey);
}

builder.Services.AddHttpClient<ISupabaseAdminClient, SupabaseAdminClient>(
    client => ConfigureSupabaseAdminHttpClient(builder, client));

// Same Supabase gateway + service-role auth, used for avatar uploads to Storage.
builder.Services.AddHttpClient<ISupabaseStorageClient, SupabaseStorageClient>(
    client => ConfigureSupabaseAdminHttpClient(builder, client));

// Create the public avatars bucket once on boot (idempotent).
builder.Services.AddHostedService<AvatarBucketInitializer>();

builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IConnectionService, ConnectionService>();
builder.Services.AddScoped<IPulseService, PulseService>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Locally and on the homeserver compose deployment the schema is applied on boot
// (Aspire has already waited for Postgres; the AppHost sets Database__MigrateOnStartup
// when publishing). Real production should apply migrations during deployment instead.
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<PulseDbContext>().Database.MigrateAsync();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<UserProvisioningMiddleware>();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
