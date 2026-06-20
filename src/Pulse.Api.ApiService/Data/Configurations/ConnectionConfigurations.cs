using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pulse.Api.ApiService.Domain;

namespace Pulse.Api.ApiService.Data.Configurations;

public class ConnectionConfiguration : IEntityTypeConfiguration<Connection>
{
    public void Configure(EntityTypeBuilder<Connection> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(c => c.InviteCode).HasMaxLength(16);

        // An invite code is only meaningful while an invite is outstanding, so the unique index is
        // filtered: many connections can have a null code (already accepted or cancelled), but a live
        // code is globally unique so it resolves to exactly one connection when a partner enters it.
        builder.HasIndex(c => c.InviteCode).IsUnique().HasFilter("invite_code IS NOT NULL");

        // Fast lookup of "my connection" from either side.
        builder.HasIndex(c => c.UserAId);
        builder.HasIndex(c => c.UserBId);

        builder.HasOne(c => c.UserA)
            .WithMany()
            .HasForeignKey(c => c.UserAId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.UserB)
            .WithMany()
            .HasForeignKey(c => c.UserBId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(c => c.IsActive);
    }
}
