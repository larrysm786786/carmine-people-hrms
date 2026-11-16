using CarminePeople.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarminePeople.Infrastructure.Persistence.Configurations;

public class JobPostingConfiguration : IEntityTypeConfiguration<JobPosting>
{
    public void Configure(EntityTypeBuilder<JobPosting> builder)
    {
        builder.Property(j => j.Title).HasMaxLength(200).IsRequired();
        builder.Property(j => j.Department).HasMaxLength(100).IsRequired();
        builder.Property(j => j.Description).HasMaxLength(5000);
    }
}

public class CandidateConfiguration : IEntityTypeConfiguration<Candidate>
{
    public void Configure(EntityTypeBuilder<Candidate> builder)
    {
        builder.Property(c => c.FullName).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.Property(c => c.ResumeUrl).HasMaxLength(500);
        builder.Property(c => c.Notes).HasMaxLength(2000);

        builder.HasOne(c => c.JobPosting)
            .WithMany(j => j.Candidates)
            .HasForeignKey(c => c.JobPostingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
