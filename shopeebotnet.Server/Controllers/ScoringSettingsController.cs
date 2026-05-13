using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shopeebotnet.Server.DbContext;
using shopeebotnet.Server.Models;

namespace shopeebotnet.Server.Controllers;

[ApiController]
[Route("api/scoring-settings")]
public class ScoringSettingsController : ControllerBase
{
    private readonly ShopeeAffiliateContext _db;

    public ScoringSettingsController(ShopeeAffiliateContext db)
    {
        _db = db;
    }

    public record ScoringSettingDto(
        Guid Id,
        string Weights,
        DateOnly? ActiveFrom,
        DateOnly? ActiveTo,
        Guid? CreatedByUserId,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );

    public record GetCurrentResponse(ScoringSettingDto Setting);

    public record CreateScoringSettingRequest(
        string Weights,
        DateOnly? ActiveFrom,
        DateOnly? ActiveTo
    );

    [Authorize]
    [HttpGet("current")]
    public async Task<ActionResult<GetCurrentResponse>> GetCurrent(
        [FromQuery] string? date
    )
    {
        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow);

        if (!string.IsNullOrWhiteSpace(date))
        {
            if (!DateOnly.TryParseExact(date.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out targetDate))
                return BadRequest(new { message = "Invalid 'date'. Expected format: yyyy-MM-dd" });
        }

        var setting = await _db.ScoringSettings.AsNoTracking()
            .Where(s =>
                (s.ActiveFrom == null || s.ActiveFrom <= targetDate) &&
                (s.ActiveTo == null || s.ActiveTo >= targetDate)
            )
            .OrderByDescending(s => s.ActiveFrom ?? DateOnly.MinValue)
            .ThenByDescending(s => s.UpdatedAt)
            .FirstOrDefaultAsync();

        if (setting == null)
            return NotFound(new { message = "No active scoring setting found for the given date." });

        return Ok(new GetCurrentResponse(ToDto(setting)));
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<ActionResult<ScoringSettingDto>> Create([FromBody] CreateScoringSettingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Weights))
            return BadRequest(new { message = "Weights is required." });

        if (request.ActiveFrom != null && request.ActiveTo != null && request.ActiveFrom > request.ActiveTo)
            return BadRequest(new { message = "ActiveFrom must be <= ActiveTo." });

        var userId = TryGetUserId();
        if (userId == null)
            return Unauthorized(new { message = "Missing user id in token." });

        var now = DateTime.UtcNow;

        var entity = new ScoringSettingModel
        {
            Id = Guid.NewGuid(),
            Weights = request.Weights.Trim(),
            ActiveFrom = request.ActiveFrom,
            ActiveTo = request.ActiveTo,
            CreatedByUserId = userId,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.ScoringSettings.Add(entity);
        await _db.SaveChangesAsync();

        return Ok(ToDto(entity));
    }

    private Guid? TryGetUserId()
    {
        var sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private static ScoringSettingDto ToDto(ScoringSettingModel s) =>
        new(
            s.Id,
            s.Weights,
            s.ActiveFrom,
            s.ActiveTo,
            s.CreatedByUserId,
            s.CreatedAt,
            s.UpdatedAt
        );
}
