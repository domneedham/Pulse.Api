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
    public DbSet<PulseTouch> PulseTouches => Set<PulseTouch>();
    public DbSet<UserFavorite> UserFavorites => Set<UserFavorite>();
    public DbSet<Pack> Packs => Set<Pack>();
    public DbSet<ConnectionPack> ConnectionPacks => Set<ConnectionPack>();
    public DbSet<MomentTemplate> MomentTemplates => Set<MomentTemplate>();
    public DbSet<Moment> Moments => Set<Moment>();
    public DbSet<MomentResponse> MomentResponses => Set<MomentResponse>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PulseDbContext).Assembly);
    }
}
