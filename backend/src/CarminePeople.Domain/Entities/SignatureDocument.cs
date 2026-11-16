using CarminePeople.Domain.Common;

namespace CarminePeople.Domain.Entities;

public class SignatureDocument : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<DocumentSignature> Signatures { get; set; } = new List<DocumentSignature>();
}

public class DocumentSignature : AuditableEntity
{
    public Guid SignatureDocumentId { get; set; }
    public SignatureDocument SignatureDocument { get; set; } = null!;

    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    // Base64-encoded PNG of the hand-drawn signature stroke — small enough (a few KB) to
    // store inline rather than wiring up blob storage for what is effectively proof text.
    public string SignatureImageBase64 { get; set; } = string.Empty;
    public DateTime SignedAt { get; set; }
}
