using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pulse.Api.ApiService.Domain;
using PulseEntity = Pulse.Api.ApiService.Domain.Pulse;

namespace Pulse.Api.ApiService.Data.Configurations;

public class PulseConfiguration : IEntityTypeConfiguration<PulseEntity>
{
    public void Configure(EntityTypeBuilder<PulseEntity> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Type).HasConversion<string>().HasMaxLength(16);

        // The timeline reads "this connection's pulses, newest first".
        builder.HasIndex(p => new { p.ConnectionId, p.CreatedAt });

        builder.HasOne(p => p.Connection)
            .WithMany(c => c.Pulses)
            .HasForeignKey(p => p.ConnectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Sender)
            .WithMany()
            .HasForeignKey(p => p.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        // Detail rows are 1:1 with the pulse, keyed by PulseId, and removed with it.
        builder.HasOne(p => p.Mood).WithOne(m => m.Pulse).HasForeignKey<PulseMood>(m => m.PulseId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(p => p.Need).WithOne(n => n.Pulse).HasForeignKey<PulseNeed>(n => n.PulseId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(p => p.Thought).WithOne(t => t.Pulse).HasForeignKey<PulseThought>(t => t.PulseId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PulseMoodConfiguration : IEntityTypeConfiguration<PulseMood>
{
    public void Configure(EntityTypeBuilder<PulseMood> builder)
    {
        builder.HasKey(m => m.PulseId);
        builder.Property(m => m.PulseId).ValueGeneratedNever();
        builder.Property(m => m.MoodType).HasConversion<string>().HasMaxLength(16);
    }
}

public class PulseNeedConfiguration : IEntityTypeConfiguration<PulseNeed>
{
    public void Configure(EntityTypeBuilder<PulseNeed> builder)
    {
        builder.HasKey(n => n.PulseId);
        builder.Property(n => n.PulseId).ValueGeneratedNever();
        builder.Property(n => n.NeedType).HasConversion<string>().HasMaxLength(16);
    }
}

public class PulseThoughtConfiguration : IEntityTypeConfiguration<PulseThought>
{
    public void Configure(EntityTypeBuilder<PulseThought> builder)
    {
        builder.HasKey(t => t.PulseId);
        builder.Property(t => t.PulseId).ValueGeneratedNever();
        builder.Property(t => t.Message).HasMaxLength(50);
    }
}
