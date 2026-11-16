using CarminePeople.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarminePeople.Infrastructure.Persistence.Configurations;

public class SignatureDocumentConfiguration : IEntityTypeConfiguration<SignatureDocument>
{
    public void Configure(EntityTypeBuilder<SignatureDocument> builder)
    {
        builder.Property(d => d.Title).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Content).HasMaxLength(10000).IsRequired();
    }
}

public class DocumentSignatureConfiguration : IEntityTypeConfiguration<DocumentSignature>
{
    public void Configure(EntityTypeBuilder<DocumentSignature> builder)
    {
        builder.Property(s => s.SignatureImageBase64).HasColumnType("text").IsRequired();

        builder.HasOne(s => s.SignatureDocument)
            .WithMany(d => d.Signatures)
            .HasForeignKey(s => s.SignatureDocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Employee)
            .WithMany()
            .HasForeignKey(s => s.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.SignatureDocumentId, s.EmployeeId }).IsUnique();
    }
}
