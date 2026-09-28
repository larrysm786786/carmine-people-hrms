using CarminePeople.Domain.Enums;

namespace CarminePeople.Application.Documents.DTOs;

public record EmployeeDocumentDto(
    Guid Id, Guid EmployeeId, DocumentType DocumentType, string FileName, long FileSizeBytes, string ContentType, DateTime CreatedAt);

public record DocumentDownloadDto(string Url, DateTime ExpiresAt);
