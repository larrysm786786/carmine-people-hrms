using CarminePeople.Domain.Common;
using CarminePeople.Domain.Enums;

namespace CarminePeople.Domain.Entities;

public class EmployeeDocument : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DocumentType DocumentType { get; set; }

    // Opaque storage key resolved by IDocumentStorage. Never a public URL.
    public string StoragePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string ContentType { get; set; } = string.Empty;

    public Guid UploadedByEmployeeId { get; set; }
    public Employee UploadedByEmployee { get; set; } = null!;
}
