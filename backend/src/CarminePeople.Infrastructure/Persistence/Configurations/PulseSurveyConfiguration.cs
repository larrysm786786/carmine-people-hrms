using CarminePeople.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarminePeople.Infrastructure.Persistence.Configurations;

public class PulseSurveyConfiguration : IEntityTypeConfiguration<PulseSurvey>
{
    public void Configure(EntityTypeBuilder<PulseSurvey> builder)
    {
        builder.Property(s => s.Question).HasMaxLength(500).IsRequired();
    }
}

public class PulseSurveyResponseConfiguration : IEntityTypeConfiguration<PulseSurveyResponse>
{
    public void Configure(EntityTypeBuilder<PulseSurveyResponse> builder)
    {
        builder.Property(r => r.Comment).HasMaxLength(1000);

        builder.HasOne(r => r.Survey)
            .WithMany(s => s.Responses)
            .HasForeignKey(r => r.SurveyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Employee)
            .WithMany()
            .HasForeignKey(r => r.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Enforces "one response per employee per survey" at the database level too,
        // not just in the service's pre-check.
        builder.HasIndex(r => new { r.SurveyId, r.EmployeeId }).IsUnique();
    }
}
