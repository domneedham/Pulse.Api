using Microsoft.EntityFrameworkCore;
using Pulse.Api.ApiService.Domain;
using PulseEntity = Pulse.Api.ApiService.Domain.Pulse;

namespace Pulse.Api.ApiService.Data;

public class PulseDbContext(DbContextOptions<PulseDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserDevice> UserDevices => Set<UserDevice>();
    public DbSet<Connection> Connections => Set<Connection>();
    public DbSet<PulseEntity> Pulses => Set<PulseEntity>();
    public DbSet<PulseMood> PulseMoods => Set<PulseMood>();
    public DbSet<PulseNeed> PulseNeeds => Set<PulseNeed>();
    public DbSet<PulseThought> PulseThoughts => Set<PulseThought>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PulseDbContext).Assembly);
    }
}
