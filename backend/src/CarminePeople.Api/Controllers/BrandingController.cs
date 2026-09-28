using Asp.Versioning;
using CarminePeople.Domain.Constants;
using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Api.Controllers;

public record BrandingDto(
    string CompanyName,
    string AppName,
    string? LogoUrl,
    string ColorGray,
    string ColorPrimary,
    string ColorBlack,
    string ColorWhite,
    string GradientCss,
    string TimeZoneId);

public record UpdateBrandingRequest(
    string CompanyName,
    string AppName,
    string? LogoUrl,
    string ColorGray,
    string ColorPrimary,
    string ColorBlack,
    string ColorWhite,
    string GradientCss,
    string TimeZoneId);

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/branding")]
public class BrandingController : ControllerBase
{
    private readonly IApplicationDbContext _db;

    public BrandingController(IApplicationDbContext db)
    {
        _db = db;
    }

    // Public: the login screen and app splash need branding before authentication.
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var b = await _db.BrandingSettings.FirstOrDefaultAsync(ct);
        b ??= new BrandingSettings();
        return Ok(new
        {
            success = true,
            data = new BrandingDto(b.CompanyName, b.AppName, b.LogoUrl, b.ColorGray, b.ColorPrimary, b.ColorBlack, b.ColorWhite, b.GradientCss, b.TimeZoneId)
        });
    }

    [HttpPut]
    [Authorize(Policy = Permissions.CompanyManage)]
    public async Task<IActionResult> Update([FromBody] UpdateBrandingRequest request, CancellationToken ct)
    {
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(request.TimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return BadRequest(new { success = false, error = new { code = "INVALID_TIMEZONE", message = $"'{request.TimeZoneId}' is not a recognized time zone ID." } });
        }

        var b = await _db.BrandingSettings.FirstOrDefaultAsync(ct);
        if (b is null)
        {
            b = new BrandingSettings();
            _db.BrandingSettings.Add(b);
        }

        b.CompanyName = request.CompanyName;
        b.AppName = request.AppName;
        b.LogoUrl = request.LogoUrl;
        b.ColorGray = request.ColorGray;
        b.ColorPrimary = request.ColorPrimary;
        b.ColorBlack = request.ColorBlack;
        b.ColorWhite = request.ColorWhite;
        b.GradientCss = request.GradientCss;
        b.TimeZoneId = request.TimeZoneId;

        await _db.SaveChangesAsync(ct);
        return Ok(new
        {
            success = true,
            data = new BrandingDto(b.CompanyName, b.AppName, b.LogoUrl, b.ColorGray, b.ColorPrimary, b.ColorBlack, b.ColorWhite, b.GradientCss, b.TimeZoneId)
        });
    }
}
