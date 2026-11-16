using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Application.Common.Models;
using CarminePeople.Application.Signatures.DTOs;
using CarminePeople.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Application.Signatures.Services;

public class SignatureDocumentService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditLogger _auditLogger;

    public SignatureDocumentService(IApplicationDbContext db, IAuditLogger auditLogger)
    {
        _db = db;
        _auditLogger = auditLogger;
    }

    public async Task<SignatureDocumentDto> CreateAsync(CreateSignatureDocumentRequest request, Guid actorId, CancellationToken ct)
    {
        var document = new SignatureDocument { Title = request.Title, Content = request.Content, CreatedBy = actorId };
        _db.SignatureDocuments.Add(document);
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "signature_document.created", "SignatureDocument", document.Id.ToString(), null, ct);
        return new SignatureDocumentDto(document.Id, document.Title, document.Content, document.IsActive, 0, document.CreatedAt);
    }

    public async Task<List<SignatureDocumentDto>> GetListAsync(CancellationToken ct) =>
        await _db.SignatureDocuments.AsNoTracking()
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new SignatureDocumentDto(d.Id, d.Title, d.Content, d.IsActive, d.Signatures.Count, d.CreatedAt))
            .ToListAsync(ct);

    public async Task<Result<List<DocumentSignatureDto>>> GetSignaturesAsync(Guid documentId, CancellationToken ct)
    {
        if (!await _db.SignatureDocuments.AnyAsync(d => d.Id == documentId, ct))
        {
            return Result<List<DocumentSignatureDto>>.Fail("NOT_FOUND", "Document not found.");
        }

        var signatures = await _db.DocumentSignatures.AsNoTracking()
            .Where(s => s.SignatureDocumentId == documentId)
            .OrderByDescending(s => s.SignedAt)
            .Select(s => new DocumentSignatureDto(s.Id, s.Employee.FirstName + " " + s.Employee.LastName, s.SignedAt))
            .ToListAsync(ct);

        return Result<List<DocumentSignatureDto>>.Ok(signatures);
    }

    public async Task<Result<bool>> CloseAsync(Guid id, Guid actorId, CancellationToken ct)
    {
        var document = await _db.SignatureDocuments.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (document is null) return Result<bool>.Fail("NOT_FOUND", "Document not found.");

        document.IsActive = false;
        document.UpdatedBy = actorId;
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "signature_document.closed", "SignatureDocument", document.Id.ToString(), null, ct);
        return Result<bool>.Ok(true);
    }

    public async Task<List<SignatureDocumentDto>> GetPendingForEmployeeAsync(Guid employeeId, CancellationToken ct) =>
        await _db.SignatureDocuments.AsNoTracking()
            .Where(d => d.IsActive && !d.Signatures.Any(s => s.EmployeeId == employeeId))
            .OrderBy(d => d.CreatedAt)
            .Select(d => new SignatureDocumentDto(d.Id, d.Title, d.Content, d.IsActive, 0, d.CreatedAt))
            .ToListAsync(ct);

    public async Task<Result<bool>> SignAsync(Guid documentId, Guid employeeId, SubmitSignatureRequest request, CancellationToken ct)
    {
        var document = await _db.SignatureDocuments.FirstOrDefaultAsync(d => d.Id == documentId, ct);
        if (document is null) return Result<bool>.Fail("NOT_FOUND", "Document not found.");
        if (!document.IsActive) return Result<bool>.Fail("CLOSED", "This document is no longer accepting signatures.");
        if (await _db.DocumentSignatures.AnyAsync(s => s.SignatureDocumentId == documentId && s.EmployeeId == employeeId, ct))
        {
            return Result<bool>.Fail("ALREADY_SIGNED", "You have already signed this document.");
        }

        _db.DocumentSignatures.Add(new DocumentSignature
        {
            SignatureDocumentId = documentId,
            EmployeeId = employeeId,
            SignatureImageBase64 = request.SignatureImageBase64,
            SignedAt = DateTime.UtcNow,
            CreatedBy = employeeId,
        });
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(employeeId, "signature_document.signed", "SignatureDocument", documentId.ToString(), null, ct);
        return Result<bool>.Ok(true);
    }
}
