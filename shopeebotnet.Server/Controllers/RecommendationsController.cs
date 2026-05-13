using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shopeebotnet.Server.DbContext;

namespace shopeebotnet.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/recommendations")]
public class RecommendationsController : ControllerBase
{
    private readonly ShopeeAffiliateContext _db;

    public RecommendationsController(ShopeeAffiliateContext db)
    {
        _db = db;
    }

    public record ProductDto(
        Guid Id,
        string ProductIdOnPlatform,
        string Name,
        decimal Price,
        decimal OriginalPrice,
        decimal CommissionRate,
        string Category,
        int ReviewCount,
        decimal Rating,
        int? SalesVolume,
        string? ImageUrl,
        string DataSource
    );

    public record DailyRecommendationDto(
        Guid ProductId,
        Guid RecommendationId,
        double Score,
        DateOnly RecommendationDate,
        string? WeightBreakdown
    );

    public record TodayRecommendationResponse(
        ProductDto Product,
        DailyRecommendationDto Recommendation
    );

    [HttpGet("products")]
    public async Task<ActionResult<List<ProductDto>>> GetProducts(
        [FromQuery] string? category,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        limit = Math.Clamp(limit, 1, 200);
        offset = Math.Max(offset, 0);

        var query = _db.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalized = category.Trim();
            query = query.Where(p => p.Category == normalized);
        }

        var items = await query
            .OrderByDescending(p => p.UpdatedAt)
            .Skip(offset)
            .Take(limit)
            .Select(p => new ProductDto(
                p.Id,
                p.ProductIdOnPlatform,
                p.Name,
                p.Price,
                p.OriginalPrice,
                p.CommissionRate,
                p.Category,
                p.ReviewCount,
                p.Rating,
                p.SalesVolume,
                p.ImageUrl,
                p.DataSource
            ))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("today")]
    public async Task<ActionResult<List<TodayRecommendationResponse>>> GetTodayRecommendations(
        [FromQuery] string? date = null)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        DateOnly recommendationDate;
        if (string.IsNullOrWhiteSpace(date))
        {
            recommendationDate = today;
        }
        else if (!DateOnly.TryParseExact(date.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out recommendationDate))
        {
            return BadRequest(new { message = "Invalid 'date'. Expected format: yyyy-MM-dd" });
        }

        var items = await _db.DailyRecommendations.AsNoTracking()
            .Where(r => r.RecommendationDate == recommendationDate)
            .Join(
                _db.Products.AsNoTracking(),
                r => r.ProductId,
                p => p.Id,
                (r, p) => new TodayRecommendationResponse(
                    new ProductDto(
                        p.Id,
                        p.ProductIdOnPlatform,
                        p.Name,
                        p.Price,
                        p.OriginalPrice,
                        p.CommissionRate,
                        p.Category,
                        p.ReviewCount,
                        p.Rating,
                        p.SalesVolume,
                        p.ImageUrl,
                        p.DataSource
                    ),
                    new DailyRecommendationDto(
                        p.Id,
                        r.Id,
                        r.Score,
                        r.RecommendationDate,
                        r.WeightBreakdown
                    )
                ))
            .OrderByDescending(x => x.Recommendation.Score)
            .ToListAsync();

        return Ok(items);
    }
}
