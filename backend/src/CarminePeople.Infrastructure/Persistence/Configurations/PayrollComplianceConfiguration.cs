using CarminePeople.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarminePeople.Infrastructure.Persistence.Configurations;

public class PayrollComplianceSettingsConfiguration : IEntityTypeConfiguration<PayrollComplianceSettings>
{
    public void Configure(EntityTypeBuilder<PayrollComplianceSettings> builder)
    {
        builder.Property(s => s.PfEmployeeRatePercent).HasPrecision(5, 2);
        builder.Property(s => s.PfEmployerRatePercent).HasPrecision(5, 2);
        builder.Property(s => s.PfWageCeiling).HasPrecision(12, 2);
        builder.Property(s => s.EsiEmployeeRatePercent).HasPrecision(5, 2);
        builder.Property(s => s.EsiEmployerRatePercent).HasPrecision(5, 2);
        builder.Property(s => s.EsiEligibilityThreshold).HasPrecision(12, 2);
    }
}

public class TdsSlabConfiguration : IEntityTypeConfiguration<TdsSlab>
{
    public void Configure(EntityTypeBuilder<TdsSlab> builder)
    {
        builder.Property(s => s.UpToAnnualIncome).HasPrecision(12, 2);
        builder.Property(s => s.RatePercent).HasPrecision(5, 2);
    }
}
