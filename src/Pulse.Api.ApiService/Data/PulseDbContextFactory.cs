using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Pulse.Api.ApiService.Data;

/// <summary>
/// Used only by the dotnet-ef CLI to create migrations without booting the app.
/// The connection string targets the local Supabase Postgres started by Aspire.
/// </summary>
public class PulseDbContextFactory : IDesignTimeDbContextFactory<PulseDbContext>
{
    public PulseDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PulseDbContext>()
            .UseNpgsql("Host=localhost;Port=54322;Database=postgres;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention()
            .Options;

        return new PulseDbContext(options);
    }
}
