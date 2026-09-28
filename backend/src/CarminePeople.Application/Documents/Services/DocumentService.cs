using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Application.Common.Models;
using CarminePeople.Application.Documents.DTOs;
using CarminePeople.Application.Notifications.Services;
using CarminePeople.Domain.Entities;
using CarminePeople.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Application.Documents.Services;

public class DocumentService
{
    // Per security.md: allowlisted content types and a hard size ceiling, enforced server-side.
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf", "image/jpeg", "image/png",
        "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    };
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    // Self-service uploads (an employee uploading their own leave attachment or profile
    // photo) shouldn't notify the employee about their own action.
    private static readonly HashSet<DocumentType> SelfServiceTypes = new()
    {
        DocumentType.LeaveAttachment, DocumentType.ProfilePhoto, DocumentType.IdProof,
    };

    private readonly IApplicationDbContext _db;
    private readonly IDocumentStorage _storage;
    private readonly NotificationService _notificationService;
    private readonly IAuditLogger _auditLogger;

    public DocumentService(IApplicationDbContext db, IDocumentStorage storage, NotificationService notificationService, IAuditLogger auditLogger)
    {
        _db = db;
        _storage = storage;
        _notificationService = notificationService;
        _auditLogger = auditLogger;
    }

    public async Task<Result<EmployeeDocumentDto>> UploadAsync(
        Guid employeeId, Guid uploadedByEmployeeId, DocumentType documentType,
        string fileName, string contentType, long fileSizeBytes, Stream content, CancellationToken ct)
    {
        if (!AllowedContentTypes.Contains(contentType))
        {
            return Result<EmployeeDocumentDto>.Fail("UNSUPPORTED_TYPE", "This file type is not allowed.");
        }
        if (fileSizeBytes <= 0 || fileSizeBytes > MaxFileSizeBytes)
        {
            return Result<EmployeeDocumentDto>.Fail("FILE_TOO_LARGE", "File exceeds the maximum allowed size of 10 MB.");
        }
        // The client-supplied Content-Type header is trivially spoofable (e.g. a renamed
        // .html file relabeled as image/png) — verify the actual file bytes match before
        // trusting the allowlist check above.
        if (!await FileSignatureValidator.MatchesAsync(content, contentType, ct))
        {
            return Result<EmployeeDocumentDto>.Fail("CONTENT_MISMATCH", "The file's content does not match its declared type.");
        }
        if (!await _db.Employees.AnyAsync(e => e.Id == employeeId, ct))
        {
            return Result<EmployeeDocumentDto>.Fail("NOT_FOUND", "Employee not found.");
        }

        var storagePath = await _storage.SaveAsync(fileName, content, ct);

        var document = new EmployeeDocument
        {
            EmployeeId = employeeId,
            DocumentType = documentType,
            StoragePath = storagePath,
            FileName = fileName,
            FileSizeBytes = fileSizeBytes,
            ContentType = contentType,
            UploadedByEmployeeId = uploadedByEmployeeId
        };
        _db.EmployeeDocuments.Add(document);
        await _db.SaveChangesAsync(ct);

        await _auditLogger.LogAsync(uploadedByEmployeeId, "document.uploaded", "EmployeeDocument", document.Id.ToString(), new { fileName, documentType, employeeId }, ct);

        // A system-generated report (e.g. the monthly attendance PDF) always notifies the
        // employee even though uploadedByEmployeeId == employeeId — that equality here means
        // "generated on this employee's behalf by a background job", not "the employee
        // uploaded their own file", so it must not be treated as a silent self-service action
        // the way an employee's own leave-attachment/profile-photo/id-proof upload is.
        var isSystemGeneratedReport = documentType == DocumentType.AttendanceReport;
        if (isSystemGeneratedReport || (!SelfServiceTypes.Contains(documentType) && uploadedByEmployeeId != employeeId))
        {
            await _notificationService.NotifyAsync(
                employeeId, "New document uploaded", $"{fileName} was uploaded to your documents.", "document", null, ct);
        }

        return Result<EmployeeDocumentDto>.Ok(ToDto(document));
    }

    public async Task<List<EmployeeDocumentDto>> GetForEmployeeAsync(Guid employeeId, CancellationToken ct)
    {
        var documents = await _db.EmployeeDocuments
            .Where(d => d.EmployeeId == employeeId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);
        return documents.Select(ToDto).ToList();
    }

    public async Task<Result<DocumentDownloadDto>> GetDownloadUrlAsync(Guid documentId, Guid requestingEmployeeId, bool canManageDocuments, CancellationToken ct)
    {
        var document = await _db.EmployeeDocuments.FirstOrDefaultAsync(d => d.Id == documentId, ct);
        if (document is null) return Result<DocumentDownloadDto>.Fail("NOT_FOUND", "Document not found.");
        if (!CanAccess(document, requestingEmployeeId, canManageDocuments))
        {
            return Result<DocumentDownloadDto>.Fail("FORBIDDEN", "You do not have access to this document.");
        }

        // API-relative path to DocumentsController's authenticated streaming route, not an
        // absolute/public URL — the local-disk implementation has no static-file server to
        // sign a real SAS URL for, so callers must resolve this against their own API base
        // URL and attach the caller's auth header themselves (never hand this to an
        // unauthenticated external browser/launcher).
        var validFor = TimeSpan.FromMinutes(10);
        return Result<DocumentDownloadDto>.Ok(new DocumentDownloadDto($"/documents/{document.Id}/content", DateTime.UtcNow.Add(validFor)));
    }

    // Backs the actual authenticated streaming route — GetDownloadUrlAsync above only hands
    // back the URL pointing at this data, it never exposes the file directly. Both routes
    // must independently enforce access since a caller could hit /content directly without
    // ever going through GetDownloadUrlAsync.
    public async Task<Result<(Stream Content, string FileName, string ContentType)>> OpenContentAsync(Guid documentId, Guid requestingEmployeeId, bool canManageDocuments, CancellationToken ct)
    {
        var document = await _db.EmployeeDocuments.FirstOrDefaultAsync(d => d.Id == documentId, ct);
        if (document is null) return Result<(Stream, string, string)>.Fail("NOT_FOUND", "Document not found.");
        if (!CanAccess(document, requestingEmployeeId, canManageDocuments))
        {
            return Result<(Stream, string, string)>.Fail("FORBIDDEN", "You do not have access to this document.");
        }

        var stream = await _storage.OpenReadAsync(document.StoragePath, ct);
        return Result<(Stream, string, string)>.Ok((stream, document.FileName, document.ContentType));
    }

    // A document is accessible to: the employee it belongs to, whoever uploaded it (e.g. HR
    // uploading an offer letter on someone's behalf), or anyone holding document.manage.
    // Without this, any authenticated employee could download any other employee's documents
    // (ID proofs, medical certificates, salary slips) just by guessing/enumerating a GUID.
    private static bool CanAccess(EmployeeDocument document, Guid requestingEmployeeId, bool canManageDocuments) =>
        canManageDocuments || document.EmployeeId == requestingEmployeeId || document.UploadedByEmployeeId == requestingEmployeeId;

    public async Task<Result<bool>> DeleteAsync(Guid documentId, Guid actorId, CancellationToken ct)
    {
        var document = await _db.EmployeeDocuments.FirstOrDefaultAsync(d => d.Id == documentId, ct);
        if (document is null) return Result<bool>.Fail("NOT_FOUND", "Document not found.");

        document.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        await _storage.DeleteAsync(document.StoragePath, ct);
        await _auditLogger.LogAsync(actorId, "document.deleted", "EmployeeDocument", document.Id.ToString(), new { document.FileName }, ct);
        return Result<bool>.Ok(true);
    }

    private static EmployeeDocumentDto ToDto(EmployeeDocument d) => new(d.Id, d.EmployeeId, d.DocumentType, d.FileName, d.FileSizeBytes, d.ContentType, d.CreatedAt);
}
