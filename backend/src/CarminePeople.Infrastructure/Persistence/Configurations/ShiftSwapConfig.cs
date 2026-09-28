using CarminePeople.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarminePeople.Infrastructure.Persistence.Configurations;

public class ShiftSwapRequestConfig : IEntityTypeConfiguration<ShiftSwapRequest>
{
    public void Configure(EntityTypeBuilder<ShiftSwapRequest> builder)
    {
        builder.HasOne(s => s.Requester).WithMany().HasForeignKey(s => s.RequesterId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Counterpart).WithMany().HasForeignKey(s => s.CounterpartId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.DecidedByEmployee).WithMany().HasForeignKey(s => s.DecidedByEmployeeId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.RequesterId);
        builder.HasIndex(s => s.CounterpartId);
    }
}
