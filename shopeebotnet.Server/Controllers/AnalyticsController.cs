using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shopeebotnet.Server.DbContext;

namespace shopeebotnet.Server.Controllers;

[ApiController]
[Route("api/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly ShopeeAffiliateContext _db;

    public AnalyticsController(ShopeeAffiliateContext db)
    {
        _db = db;
    }

    public record LinkAnalyticsResponse(
        Guid LinkId,
        string ShortCode,
        int Clicks,
        int Conversions,
        decimal TotalCommission
    );

    public record ProductAnalyticsResponse(
        Guid ProductId,
        int Clicks,
        int Conversions,
        decimal TotalCommission
    );

    // In real life you probably want admin-only and/or role-based access.
    [Authorize]
    [HttpGet("links/{shortCode}/summary")]
    public async Task<ActionResult<LinkAnalyticsResponse>> GetLinkSummary([FromRoute] string shortCode)
    {
        if (string.IsNullOrWhiteSpace(shortCode))
            return BadRequest(new { message = "shortCode is required." });

        var normalized = shortCode.Trim();

        var link = await _db.AffiliateLinks.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ShortCode == normalized);

        if (link == null)
            return NotFound(new { message = "Affiliate link not found." });

        var clicksQuery = _db.Clicks.AsNoTracking().Where(c => c.LinkId == link.Id);
        var conversionsQuery = _db.Conversions.AsNoTracking()
            .Join(_db.Clicks.AsNoTracking(),
                conv => conv.ClickId,
                click => click.Id,
                (conv, click) => new { conv, click })
            .Where(x => x.click.LinkId == link.Id);

        var clicks = await clicksQuery.CountAsync();

        var conversionsAgg = await conversionsQuery
            .GroupBy(x => 1)
            .Select(g => new
            {
                Count = g.Count(),
                TotalCommission = g.Sum(x => x.conv.Commission)
            })
            .FirstOrDefaultAsync();

        var conversions = conversionsAgg?.Count ?? 0;
        var totalCommission = conversionsAgg?.TotalCommission ?? 0m;

        return Ok(new LinkAnalyticsResponse(link.Id, link.ShortCode, clicks, conversions, totalCommission));
    }

    [Authorize]
    [HttpGet("products/{productId}/summary")]
    public async Task<ActionResult<ProductAnalyticsResponse>> GetProductSummary([FromRoute] Guid productId)
    {
        if (productId == Guid.Empty)
            return BadRequest(new { message = "productId is required." });

        var productExists = await _db.Products.AsNoTracking().AnyAsync(x => x.Id == productId);
        if (!productExists)
            return NotFound(new { message = "Product not found." });

        var clicksQuery = _db.Clicks.AsNoTracking().Where(c => c.ProductId == productId);

        var conversionsQuery = _db.Conversions.AsNoTracking()
            .Join(_db.Clicks.AsNoTracking(),
                conv => conv.ClickId,
                click => click.Id,
                (conv, click) => new { conv, click })
            .Where(x => x.click.ProductId == productId);

        var clicks = await clicksQuery.CountAsync();

        var conversionsAgg = await conversionsQuery
            .GroupBy(x => 1)
            .Select(g => new
            {
                Count = g.Count(),
                TotalCommission = g.Sum(x => x.conv.Commission)
            })
            .FirstOrDefaultAsync();

        var conversions = conversionsAgg?.Count ?? 0;
        var totalCommission = conversionsAgg?.TotalCommission ?? 0m;

        return Ok(new ProductAnalyticsResponse(productId, clicks, conversions, totalCommission));
    }
}
