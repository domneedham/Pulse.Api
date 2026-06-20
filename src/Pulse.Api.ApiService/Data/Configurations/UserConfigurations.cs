using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pulse.Api.ApiService.Domain;

namespace Pulse.Api.ApiService.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        // Id comes from Supabase Auth (the JWT "sub" claim), never generated here.
        builder.Property(u => u.Id).ValueGeneratedNever();
        builder.Property(u => u.DisplayName).HasMaxLength(80);
        builder.Property(u => u.AvatarUrl).HasMaxLength(2048);
        builder.Property(u => u.Timezone).HasMaxLength(64);
        // Usernames are stored lowercased (set via UserService), so a plain unique index gives
        // case-insensitive uniqueness. Filtered so multiple nulls (pre-set) don't collide.
        builder.Property(u => u.Username).HasMaxLength(30);
        builder.HasIndex(u => u.Username).IsUnique().HasFilter("username IS NOT NULL");
    }
}

public class UserDeviceConfiguration : IEntityTypeConfiguration<UserDevice>
{
    public void Configure(EntityTypeBuilder<UserDevice> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.FcmToken).HasMaxLength(512);
        builder.Property(d => d.Platform).HasConversion<string>().HasMaxLength(16);
        builder.Property(d => d.DeviceModel).HasMaxLength(128);
        builder.Property(d => d.DeviceName).HasMaxLength(128);
        builder.Property(d => d.OsVersion).HasMaxLength(64);
        builder.Property(d => d.AppVersion).HasMaxLength(32);

        // A token identifies one physical app install; registering it again re-homes it.
        builder.HasIndex(d => d.FcmToken).IsUnique();
        builder.HasIndex(d => d.UserId);

        builder.HasOne(d => d.User)
            .WithMany(u => u.Devices)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
