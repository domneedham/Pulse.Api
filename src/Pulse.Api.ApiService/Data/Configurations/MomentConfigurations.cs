using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Pulse.Api.ApiService.Domain;

namespace Pulse.Api.ApiService.Data.Configurations;

public class PackConfiguration : IEntityTypeConfiguration<Pack>
{
    public void Configure(EntityTypeBuilder<Pack> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Key).HasMaxLength(40);
        builder.Property(p => p.Title).HasMaxLength(60);
        builder.Property(p => p.Emoji).HasMaxLength(16);
        builder.HasIndex(p => p.Key).IsUnique();

        // Built-in catalogue — deterministic ids so the seed is stable across migrations.
        builder.HasData(MomentCatalog.Packs.Select(p => new Pack
        {
            Id = p.Id,
            Key = p.Key,
            Title = p.Title,
            Emoji = p.Emoji,
            IsPro = p.IsPro,
            SortOrder = p.SortOrder
        }));
    }
}

public class ConnectionPackConfiguration : IEntityTypeConfiguration<ConnectionPack>
{
    public void Configure(EntityTypeBuilder<ConnectionPack> builder)
    {
        builder.HasKey(cp => new { cp.ConnectionId, cp.PackId });

        builder.HasOne(cp => cp.Connection)
            .WithMany()
            .HasForeignKey(cp => cp.ConnectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cp => cp.Pack)
            .WithMany()
            .HasForeignKey(cp => cp.PackId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class MomentTemplateConfiguration : IEntityTypeConfiguration<MomentTemplate>
{
    public void Configure(EntityTypeBuilder<MomentTemplate> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Category).HasConversion<string>().HasMaxLength(16);
        builder.Property(t => t.ResponseKind).HasConversion<string>().HasMaxLength(16);
        builder.Property(t => t.Title).HasMaxLength(60);
        builder.Property(t => t.Prompt).HasMaxLength(200);

        // Choice options stored as a JSON array (jsonb). Value comparer so EF tracks list changes.
        var optionsConverter = new ValueConverter<IReadOnlyList<string>?, string?>(
            v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => v == null ? null : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null));
        var optionsComparer = new ValueComparer<IReadOnlyList<string>?>(
            (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
            v => v == null ? 0 : v.Aggregate(0, (h, s) => HashCode.Combine(h, s.GetHashCode())),
            v => v == null ? null : v.ToList());
        builder.Property(t => t.Options)
            .HasConversion(optionsConverter)
            .Metadata.SetValueComparer(optionsComparer);
        builder.Property(t => t.Options).HasColumnType("jsonb");

        builder.HasOne(t => t.Pack)
            .WithMany(p => p.Templates)
            .HasForeignKey(t => t.PackId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.PackId);

        builder.HasData(MomentCatalog.Templates.Select(t => new MomentTemplate
        {
            Id = t.Id,
            PackId = t.PackId,
            Category = t.Category,
            Title = t.Title,
            Prompt = t.Prompt,
            ResponseKind = t.ResponseKind,
            Options = t.Options
        }));
    }
}

public class MomentConfiguration : IEntityTypeConfiguration<Moment>
{
    public void Configure(EntityTypeBuilder<Moment> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Ignore(m => m.IsComplete);

        // At most one Moment per connection per day; the Trail reads them newest-first.
        builder.HasIndex(m => new { m.ConnectionId, m.Date }).IsUnique();

        // The job/recompute scan connections by their latest assignment.
        builder.HasIndex(m => new { m.ConnectionId, m.SequenceNumber });

        builder.HasOne(m => m.Connection)
            .WithMany()
            .HasForeignKey(m => m.ConnectionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Templates are catalogue data — never delete a template out from under a live Moment.
        builder.HasOne(m => m.Template)
            .WithMany()
            .HasForeignKey(m => m.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class MomentResponseConfiguration : IEntityTypeConfiguration<MomentResponse>
{
    public void Configure(EntityTypeBuilder<MomentResponse> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Kind).HasConversion<string>().HasMaxLength(16);
        builder.Property(r => r.Text).HasMaxLength(280);
        builder.Property(r => r.Emoji).HasMaxLength(16);
        // Vector strokes stored as jsonb, same as PulseTouch.
        builder.Property(r => r.StrokeData).HasColumnType("jsonb");
        builder.Property(r => r.PhotoPath).HasMaxLength(400);
        builder.Property(r => r.PhotoUrl).HasMaxLength(800);
        builder.Property(r => r.VoicePath).HasMaxLength(400);
        builder.Property(r => r.VoiceUrl).HasMaxLength(800);

        // One response per partner per Moment.
        builder.HasIndex(r => new { r.MomentId, r.UserId }).IsUnique();

        builder.HasOne(r => r.Moment)
            .WithMany(m => m.Responses)
            .HasForeignKey(r => r.MomentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
