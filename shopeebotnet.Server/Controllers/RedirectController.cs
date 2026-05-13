using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shopeebotnet.Server.DbContext;
using shopeebotnet.Server.Models;

namespace shopeebotnet.Server.Controllers;

[ApiController]
public class RedirectController : ControllerBase
{
    private readonly ShopeeAffiliateContext _db;

    public RedirectController(ShopeeAffiliateContext db)
    {
        _db = db;
    }

    [HttpGet("r/{shortCode}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> RedirectToOriginal([FromRoute] string shortCode)
    {
        if (string.IsNullOrWhiteSpace(shortCode))
            return BadRequest(new { message = "shortCode is required." });

        var normalized = shortCode.Trim();

        var link = await _db.AffiliateLinks.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ShortCode == normalized);

        if (link == null)
            return NotFound(new { message = "Affiliate link not found." });

        // Async click logging (request-scoped; placeholder for background queue later)
        await LogClickAsync(link);

        if (string.IsNullOrWhiteSpace(link.OriginalUrl))
            return StatusCode(500, new { message = "Affiliate link original URL is missing." });

        return Redirect(link.OriginalUrl);
    }

    private async Task LogClickAsync(AffiliateLinkModel link)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();
        var trafficSource =
            Request.Query.TryGetValue("traffic_source", out var values) && values.Count > 0
                ? values[0]
                : null;

        var click = new ClickModel
        {
            Id = Guid.NewGuid(),
            LinkId = link.Id,
            ProductId = link.ProductId,
            Timestamp = DateTime.UtcNow,
            Ip = ip,
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent,
            TrafficSource = string.IsNullOrWhiteSpace(trafficSource) ? null : trafficSource,
            Converted = false
        };

        _db.Clicks.Add(click);
        await _db.SaveChangesAsync();
    }
}
