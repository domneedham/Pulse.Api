using Aspire.Hosting.Azure;
using Nextended.Aspire.Hosting.Supabase.Builders;

var builder = DistributedApplication.CreateBuilder(args);

// Publish target: `aspire publish`/`aspire deploy` emit docker-compose.yaml + .env for the homeserver.
builder.AddDockerComposeEnvironment("homeserver")
    .ConfigureComposeFile(file =>
    {
        foreach (var (name, service) in file.Services)
        {
            // supabase-init is a one-shot job; a restart policy would re-run it forever.
            if (!name.EndsWith("-init"))
            {
                service.Restart ??= "unless-stopped";
            }
        }
    });

// Full local Supabase stack: Postgres, GoTrue (auth), PostgREST, Storage, Kong, Studio.
var supabase = builder.AddSupabase("supabase")
    .ConfigureAuth(auth => auth
        .WithAutoConfirm(true)
        .WithDisableSignup(false))
    .ConfigureStudio(studio => studio
        .WithOrganizationName("Pulse")
        .WithProjectName("Pulse"))
    // Kong is the public Supabase entry point; the mobile client talks to it directly,
    // so it must listen on all interfaces (LAN) locally and be published in Compose.
    .ConfigureKong(kong =>
    {
        kong.WithExternalHttpEndpoints();
        if (builder.ExecutionContext.IsRunMode)
        {
            kong.WithEndpoint("http", e => e.TargetHost = "0.0.0.0", createIfNotExists: false);
        }
        else
        {
            // Fixed host port on the homeserver so the mobile client has a stable Supabase URL
            // (8000/8080 are taken on the homeserver; container side stays 8000).
            kong.PublishAsDockerComposeService((_, service) => service.Ports = ["7079:8000"]);
        }
    })
    .WithRegisteredUser("dom@pulse.dev", "Pulse123!", "Dom")
    .WithRegisteredUser("mike@pulse.dev", "Pulse123!", "Mike")
    .WithRegisteredUser("sarah@pulse.dev", "Pulse123!", "Sarah")
    .WithClearCommand();

var apiservice = builder.AddProject<Projects.Pulse_Api_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    // Injects ConnectionStrings__supabase__Url (Kong gateway) and __Key (anon key).
    .WithSupabaseReference(supabase)
    // EF Core connects straight to the Supabase Postgres, bypassing PostgREST.
    .WithEnvironment(ctx =>
    {
        var pulsedb = supabase.Resource.GetPostgresConnectionString();
        if (ctx.ExecutionContext.IsPublishMode)
        {
            // GetPostgresConnectionString targets host-side dev tooling (localhost + the
            // externally mapped port); inside the compose network the db is supabase-db:5432.
            pulsedb = System.Text.RegularExpressions.Regex.Replace(
                pulsedb, @"Host=[^;]+;Port=\d+", $"Host={supabase.GetDatabase().Resource.Name};Port=5432");

            // The compose deployment runs as Production, where migrations don't run on
            // boot by default; the homeserver has no separate deployment step, so opt in.
            ctx.EnvironmentVariables["Database__MigrateOnStartup"] = "true";

            // The homeserver is the test/staging box, so the dev-only simulated-purchase endpoint
            // (POST /api/tokens/dev/simulate-purchase) is enabled there to exercise the token flow
            // without a paid Apple/Google account. Overridable at deploy time
            // (Tokens__EnableSimulatedPurchases=false) and MUST be off for a real production deploy.
            ctx.EnvironmentVariables["Tokens__EnableSimulatedPurchases"] =
                Environment.GetEnvironmentVariable("Tokens__EnableSimulatedPurchases") ?? "true";
        }
        ctx.EnvironmentVariables["ConnectionStrings__pulsedb"] = pulsedb;
        ctx.EnvironmentVariables["Supabase__JwtSecret"] = supabase.Resource.JwtSecret;
        ctx.EnvironmentVariables["Supabase__ServiceRoleKey"] = supabase.Resource.ServiceRoleKey;
    })
    .WaitFor(supabase);

if (builder.ExecutionContext.IsRunMode)
{
    // Bind the API on all interfaces so the mobile client can reach it over the LAN.
    apiservice.WithEndpoint("http", e => e.TargetHost = "0.0.0.0", createIfNotExists: false);
}
else
{
    // Fixed host port + listen port on the homeserver so the mobile client has a stable API URL.
    apiservice
        .WithEndpoint("http", e => e.TargetPort = 8080, createIfNotExists: false)
        .PublishAsDockerComposeService((_, service) => service.Ports = ["7080:8080"]);

    // Persist Postgres + uploaded files under /opt/pulse on the docker host (browsable,
    // rsync-able, survives `compose down -v`; scrapping = rm -rf /opt/pulse on the server).
    supabase.GetDatabase().WithBindMount("/opt/pulse/db", "/var/lib/postgresql/data");

    // The package gives storage a named volume; replace it with the bind mount.
    var storage = supabase.GetStorage();
    foreach (var mount in storage.Resource.Annotations.OfType<ContainerMountAnnotation>()
                 .Where(m => m.Type == ContainerMountType.Volume).ToList())
    {
        storage.Resource.Annotations.Remove(mount);
    }
    storage.WithBindMount("/opt/pulse/storage", "/var/lib/storage");

    // DbGate: a web DB browser for poking at the homeserver Postgres. Part of the Aspire
    // deployment so it lands in the same compose project/network automatically — no manual
    // network-name chasing. Reachable at http://<homeserver>:7078; its preconfigured "pulse"
    // connection points straight at supabase-db. Settings persist under /opt/pulse/dbgate.
    var dbResource = supabase.Resource.Database!.Resource;
    builder.AddContainer("dbgate", "dbgate/dbgate", "latest")
        .WithHttpEndpoint(targetPort: 3000, name: "http")
        .WithBindMount("/opt/pulse/dbgate", "/root/.dbgate")
        .WithEnvironment("CONNECTIONS", "pulse")
        .WithEnvironment("LABEL_pulse", "Pulse Supabase")
        .WithEnvironment("SERVER_pulse", dbResource.Name)
        .WithEnvironment("PORT_pulse", "5432")
        .WithEnvironment("USER_pulse", "supabase_admin")
        .WithEnvironment("PASSWORD_pulse", dbResource.Password)
        .WithEnvironment("DATABASE_pulse", "postgres")
        .WithEnvironment("ENGINE_pulse", "postgres@dbgate-plugin-postgres")
        .WaitFor(supabase.GetDatabase()!)
        // Fixed host port on the homeserver, matching the old standalone compose file.
        .PublishAsDockerComposeService((_, service) => service.Ports = ["7078:3000"]);

    // The Supabase package hardcodes PublishAsAzureContainerApp on its containers, which
    // fails validation when publishing to Docker Compose. Strip those annotations so the
    // resources land in the compose environment instead.
    foreach (var resource in builder.Resources)
    {
        foreach (var annotation in resource.Annotations.OfType<AzureContainerAppCustomizationAnnotation>().ToList())
        {
            resource.Annotations.Remove(annotation);
        }
    }

}

builder.Build().Run();
