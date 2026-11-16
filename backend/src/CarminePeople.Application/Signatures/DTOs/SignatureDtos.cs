namespace CarminePeople.Application.Signatures.DTOs;

public record SignatureDocumentDto(Guid Id, string Title, string Content, bool IsActive, int SignatureCount, DateTime CreatedAt);

public record CreateSignatureDocumentRequest(string Title, string Content);

public record DocumentSignatureDto(Guid Id, string EmployeeName, DateTime SignedAt);

public record SubmitSignatureRequest(string SignatureImageBase64);
