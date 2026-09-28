using Asp.Versioning;
using CarminePeople.Domain.Constants;
using CarminePeople.Api.Common;
using CarminePeople.Application.Documents.Services;
using CarminePeople.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarminePeople.Api.Controllers;

public class UploadDocumentForm
{
    public Guid EmployeeId { get; set; }
    public DocumentType DocumentType { get; set; }
    public IFormFile File { get; set; } = null!;
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/documents")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly DocumentService _documentService;

    public DocumentsController(DocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpPost("upload")]
    [Authorize(Policy = Permissions.DocumentManage)]
    public async Task<IActionResult> Upload([FromForm] UploadDocumentForm form, CancellationToken ct)
    {
        if (form.File.Length == 0) return BadRequest(Envelope("EMPTY_FILE", "The uploaded file is empty."));

        await using var stream = form.File.OpenReadStream();
        var result = await _documentService.UploadAsync(
            form.EmployeeId, User.GetEmployeeId(), form.DocumentType, form.File.FileName, form.File.ContentType, form.File.Length, stream, ct);

        if (!result.Success) return BadRequest(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    // Self-service: any authenticated employee may upload an attachment for their own leave
    // application (e.g. a medical certificate) without needing document.manage, which is
    // reserved for HR-managed document types.
    [HttpPost("upload/leave-attachment")]
    public async Task<IActionResult> UploadLeaveAttachment(IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0) return BadRequest(Envelope("EMPTY_FILE", "The uploaded file is empty."));

        await using var stream = file.OpenReadStream();
        var result = await _documentService.UploadAsync(
            User.GetEmployeeId(), User.GetEmployeeId(), DocumentType.LeaveAttachment, file.FileName, file.ContentType, file.Length, stream, ct);

        if (!result.Success) return BadRequest(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    // Self-service: any authenticated employee may upload their own profile photo.
    [HttpPost("upload/profile-photo")]
    public async Task<IActionResult> UploadProfilePhoto(IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0) return BadRequest(Envelope("EMPTY_FILE", "The uploaded file is empty."));

        await using var stream = file.OpenReadStream();
        var result = await _documentService.UploadAsync(
            User.GetEmployeeId(), User.GetEmployeeId(), DocumentType.ProfilePhoto, file.FileName, file.ContentType, file.Length, stream, ct);

        if (!result.Success) return BadRequest(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    // Self-service: any authenticated employee may upload their own ID-proof document (the
    // mobile OCR auto-fill flow uploads here after the employee confirms the extracted data).
    [HttpPost("upload/id-proof")]
    public async Task<IActionResult> UploadIdProof(IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0) return BadRequest(Envelope("EMPTY_FILE", "The uploaded file is empty."));

        await using var stream = file.OpenReadStream();
        var result = await _documentService.UploadAsync(
            User.GetEmployeeId(), User.GetEmployeeId(), DocumentType.IdProof, file.FileName, file.ContentType, file.Length, stream, ct);

        if (!result.Success) return BadRequest(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpPost("upload/announcement-media")]
    [Authorize(Policy = Permissions.AnnouncementManage)]
    public async Task<IActionResult> UploadAnnouncementMedia(IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0) return BadRequest(Envelope("EMPTY_FILE", "The uploaded file is empty."));

        await using var stream = file.OpenReadStream();
        var result = await _documentService.UploadAsync(
            User.GetEmployeeId(), User.GetEmployeeId(), DocumentType.AnnouncementMedia, file.FileName, file.ContentType, file.Length, stream, ct);

        if (!result.Success) return BadRequest(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var result = await _documentService.GetDownloadUrlAsync(id, User.GetEmployeeId(), User.HasClaim("permission", Permissions.DocumentManage), ct);
        if (!result.Success) return result.ErrorCode == "FORBIDDEN" ? Forbid() : NotFound(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    // The route GetDownloadUrlAsync's URL actually points at — requires auth like every
    // other route on this controller, so the "download URL" is never a bare public path.
    // Enforces its own ownership check independently of Download() above, since a caller
    // could hit this route directly without going through GetDownloadUrlAsync first.
    [HttpGet("{id:guid}/content")]
    public async Task<IActionResult> Content(Guid id, CancellationToken ct)
    {
        var result = await _documentService.OpenContentAsync(id, User.GetEmployeeId(), User.HasClaim("permission", Permissions.DocumentManage), ct);
        if (!result.Success) return result.ErrorCode == "FORBIDDEN" ? Forbid() : NotFound(Envelope(result.ErrorCode!, result.ErrorMessage!));
        var (content, fileName, contentType) = result.Data;
        return File(content, contentType, fileName);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.DocumentManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _documentService.DeleteAsync(id, User.GetEmployeeId(), ct);
        if (!result.Success) return NotFound(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = (object?)null });
    }

    private object Envelope(string code, string message) => Api.Common.ApiEnvelope.Error(HttpContext, code, message);
}
