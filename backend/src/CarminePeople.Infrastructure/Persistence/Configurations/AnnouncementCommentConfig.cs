using CarminePeople.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarminePeople.Infrastructure.Persistence.Configurations;

public class AnnouncementCommentConfig : IEntityTypeConfiguration<AnnouncementComment>
{
    public void Configure(EntityTypeBuilder<AnnouncementComment> builder)
    {
        builder.Property(c => c.Body).HasMaxLength(1000);

        builder.HasOne(c => c.Announcement).WithMany().HasForeignKey(c => c.AnnouncementId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(c => c.Employee).WithMany().HasForeignKey(c => c.EmployeeId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.AnnouncementId);
    }
}
