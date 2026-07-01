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
        builder.Property(p => p.Reaction).HasMaxLength(16);
        builder.Property(p => p.Note).HasMaxLength(80);
        builder.Ignore(p => p.Phrase);

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
        builder.HasOne(p => p.Touch).WithOne(t => t.Pulse).HasForeignKey<PulseTouch>(t => t.PulseId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PulseTouchConfiguration : IEntityTypeConfiguration<PulseTouch>
{
    public void Configure(EntityTypeBuilder<PulseTouch> builder)
    {
        builder.HasKey(t => t.PulseId);
        builder.Property(t => t.PulseId).ValueGeneratedNever();
        // Vector strokes stored as jsonb so they can be queried/validated by Postgres if needed later.
        builder.Property(t => t.StrokeData).HasColumnType("jsonb");
    }
}

public class PulseMoodConfiguration : IEntityTypeConfiguration<PulseMood>
{
    public void Configure(EntityTypeBuilder<PulseMood> builder)
    {
        builder.HasKey(m => m.PulseId);
        builder.Property(m => m.PulseId).ValueGeneratedNever();
        builder.Property(m => m.Text).HasMaxLength(80);
        builder.Property(m => m.Emoji).HasMaxLength(16);
    }
}

public class PulseNeedConfiguration : IEntityTypeConfiguration<PulseNeed>
{
    public void Configure(EntityTypeBuilder<PulseNeed> builder)
    {
        builder.HasKey(n => n.PulseId);
        builder.Property(n => n.PulseId).ValueGeneratedNever();
        builder.Property(n => n.Text).HasMaxLength(80);
        builder.Property(n => n.Emoji).HasMaxLength(16);
    }
}

public class PulseThoughtConfiguration : IEntityTypeConfiguration<PulseThought>
{
    public void Configure(EntityTypeBuilder<PulseThought> builder)
    {
        builder.HasKey(t => t.PulseId);
        builder.Property(t => t.PulseId).ValueGeneratedNever();
        builder.Property(t => t.Text).HasMaxLength(80);
        builder.Property(t => t.Emoji).HasMaxLength(16);
    }
}

public class UserFavoriteConfiguration : IEntityTypeConfiguration<UserFavorite>
{
    public void Configure(EntityTypeBuilder<UserFavorite> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Category).HasConversion<string>().HasMaxLength(16);
        builder.Property(f => f.Text).HasMaxLength(80);
        builder.Property(f => f.Emoji).HasMaxLength(16);

        builder.HasIndex(f => new { f.UserId, f.Category, f.SortOrder });

        builder.HasOne(f => f.User)
            .WithMany()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
