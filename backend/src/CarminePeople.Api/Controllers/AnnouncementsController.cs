using Asp.Versioning;
using CarminePeople.Domain.Constants;
using CarminePeople.Api.Common;
using CarminePeople.Application.Announcements.DTOs;
using CarminePeople.Application.Announcements.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarminePeople.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/announcements")]
[Authorize]
public class AnnouncementsController : ControllerBase
{
    private readonly AnnouncementService _announcementService;

    public AnnouncementsController(AnnouncementService announcementService)
    {
        _announcementService = announcementService;
    }

    [HttpGet]
    public async Task<IActionResult> GetFeed(CancellationToken ct)
    {
        var feed = await _announcementService.GetFeedAsync(User.GetEmployeeId(), ct);
        return Ok(new { success = true, data = feed });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.AnnouncementManage)]
    public async Task<IActionResult> Create([FromBody] CreateAnnouncementRequest request, CancellationToken ct)
    {
        var result = await _announcementService.CreateAsync(User.GetEmployeeId(), request, ct);
        return Ok(new { success = true, data = result });
    }

    [HttpGet("{id:guid}/comments")]
    public async Task<IActionResult> GetComments(Guid id, CancellationToken ct)
    {
        var comments = await _announcementService.GetCommentsAsync(id, ct);
        return Ok(new { success = true, data = comments });
    }

    [HttpPost("{id:guid}/comments")]
    [Authorize(Policy = Permissions.AnnouncementComment)]
    public async Task<IActionResult> AddComment(Guid id, [FromBody] AddCommentRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return BadRequest(ApiEnvelope.Error(HttpContext, "EMPTY_COMMENT", "Comment cannot be empty."));
        }
        var comment = await _announcementService.AddCommentAsync(id, User.GetEmployeeId(), request.Body, ct);
        return Ok(new { success = true, data = comment });
    }
}
