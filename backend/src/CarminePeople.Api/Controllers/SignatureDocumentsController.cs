using Asp.Versioning;
using CarminePeople.Api.Common;
using CarminePeople.Application.Signatures.DTOs;
using CarminePeople.Application.Signatures.Services;
using CarminePeople.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarminePeople.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/signature-documents")]
[Authorize]
public class SignatureDocumentsController : ControllerBase
{
    private readonly SignatureDocumentService _signatureDocumentService;

    public SignatureDocumentsController(SignatureDocumentService signatureDocumentService)
    {
        _signatureDocumentService = signatureDocumentService;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.EmployeeManage)]
    public async Task<IActionResult> GetList(CancellationToken ct)
    {
        var documents = await _signatureDocumentService.GetListAsync(ct);
        return Ok(new { success = true, data = documents });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.EmployeeManage)]
    public async Task<IActionResult> Create([FromBody] CreateSignatureDocumentRequest request, CancellationToken ct)
    {
        var document = await _signatureDocumentService.CreateAsync(request, User.GetEmployeeId(), ct);
        return Ok(new { success = true, data = document });
    }

    [HttpGet("{id:guid}/signatures")]
    [Authorize(Policy = Permissions.EmployeeManage)]
    public async Task<IActionResult> GetSignatures(Guid id, CancellationToken ct)
    {
        var result = await _signatureDocumentService.GetSignaturesAsync(id, ct);
        if (!result.Success) return NotFound(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpPut("{id:guid}/close")]
    [Authorize(Policy = Permissions.EmployeeManage)]
    public async Task<IActionResult> Close(Guid id, CancellationToken ct)
    {
        var result = await _signatureDocumentService.CloseAsync(id, User.GetEmployeeId(), ct);
        if (!result.Success) return NotFound(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = (object?)null });
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending(CancellationToken ct)
    {
        var documents = await _signatureDocumentService.GetPendingForEmployeeAsync(User.GetEmployeeId(), ct);
        return Ok(new { success = true, data = documents });
    }

    [HttpPost("{id:guid}/sign")]
    public async Task<IActionResult> Sign(Guid id, [FromBody] SubmitSignatureRequest request, CancellationToken ct)
    {
        var result = await _signatureDocumentService.SignAsync(id, User.GetEmployeeId(), request, ct);
        if (!result.Success) return BadRequest(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = (object?)null });
    }

    private object Envelope(string code, string message) => ApiEnvelope.Error(HttpContext, code, message);
}
