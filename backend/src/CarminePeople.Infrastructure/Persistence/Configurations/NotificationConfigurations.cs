using CarminePeople.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarminePeople.Infrastructure.Persistence.Configurations;

public class AnnouncementConfig : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> builder)
    {
        builder.HasOne(a => a.CreatedByEmployee).WithMany().HasForeignKey(a => a.CreatedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(a => a.PublishAt);
    }
}

public class NotificationConfig : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasOne(n => n.Employee).WithMany().HasForeignKey(n => n.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(n => new { n.EmployeeId, n.IsRead });
    }
}

public class NotificationPreferenceConfig : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.HasIndex(p => new { p.EmployeeId, p.Channel, p.Category }).IsUnique();
        builder.HasOne(p => p.Employee).WithMany().HasForeignKey(p => p.EmployeeId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class DeviceTokenConfig : IEntityTypeConfiguration<DeviceToken>
{
    public void Configure(EntityTypeBuilder<DeviceToken> builder)
    {
        builder.HasIndex(d => d.Token).IsUnique();
        builder.HasOne(d => d.Employee).WithMany().HasForeignKey(d => d.EmployeeId).OnDelete(DeleteBehavior.Cascade);
    }
}
