using CarminePeople.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarminePeople.Infrastructure.Persistence.Configurations;

public class EmployeeDocumentConfig : IEntityTypeConfiguration<EmployeeDocument>
{
    public void Configure(EntityTypeBuilder<EmployeeDocument> builder)
    {
        builder.HasOne(d => d.Employee).WithMany().HasForeignKey(d => d.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(d => d.UploadedByEmployee).WithMany().HasForeignKey(d => d.UploadedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(d => d.ContentType).HasMaxLength(200);
        builder.Property(d => d.FileName).HasMaxLength(260);
    }
}
