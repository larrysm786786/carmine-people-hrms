using CarminePeople.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarminePeople.Infrastructure.Persistence.Configurations;

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.Property(a => a.Name).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Category).HasMaxLength(100).IsRequired();
        builder.Property(a => a.SerialNumber).HasMaxLength(100);

        builder.HasOne(a => a.AssignedToEmployee)
            .WithMany()
            .HasForeignKey(a => a.AssignedToEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
