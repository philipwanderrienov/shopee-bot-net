using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shopeebotnet.Server.DbContext;
using shopeebotnet.Server.Models;

namespace shopeebotnet.Server.Controllers;

[ApiController]
[Route("api/affiliate-links")]
public class AffiliateLinksController : ControllerBase
{
    private readonly ShopeeAffiliateContext _db;
    private readonly IConfiguration _configuration;

    public AffiliateLinksController(ShopeeAffiliateContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public record GenerateAffiliateLinkRequest(Guid ProductId, string OriginalUrl);
    public record GenerateAffiliateLinkResponse(Guid Id, string ShortCode, string ShortUrl);

    public record AffiliateLinkDto(
        Guid Id,
        Guid ProductId,
        string OriginalUrl,
        string ShortCode,
        string ShortUrl
    );

    [Authorize(Roles = "admin")]
    [HttpPost("generate")]
    public async Task<ActionResult<GenerateAffiliateLinkResponse>> Generate(
        [FromBody] GenerateAffiliateLinkRequest request)
    {
        if (request.ProductId == Guid.Empty)
            return BadRequest(new { message = "ProductId is required." });

        if (string.IsNullOrWhiteSpace(request.OriginalUrl))
            return BadRequest(new { message = "OriginalUrl is required." });

        var trackingBase = _configuration["Tracking:ShortUrlBase"];
        if (string.IsNullOrWhiteSpace(trackingBase))
            return StatusCode(500, new { message = "Tracking:ShortUrlBase is missing." });

        var now = DateTime.UtcNow;

        // Generate unique shortcode (best-effort retries)
        string shortCode = string.Empty;
        for (var i = 0; i < 10; i++)
        {
            shortCode = GenerateShortCode(10);
            var exists = await _db.AffiliateLinks.AsNoTracking().AnyAsync(x => x.ShortCode == shortCode);
            if (!exists) break;
        }

        if (string.IsNullOrWhiteSpace(shortCode))
            return StatusCode(500, new { message = "Failed to generate a unique short code." });

        var shortUrl = $"{trackingBase.TrimEnd('/')}/{shortCode}";

        var entity = new AffiliateLinkModel
        {
            Id = Guid.NewGuid(),
            ProductId = request.ProductId,
            OriginalUrl = request.OriginalUrl.Trim(),
            ShortCode = shortCode,
            ShortUrl = shortUrl,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.AffiliateLinks.Add(entity);
        await _db.SaveChangesAsync();

        return Ok(new GenerateAffiliateLinkResponse(entity.Id, entity.ShortCode, entity.ShortUrl));
    }

    [Authorize]
    [HttpGet("{shortCode}")]
    public async Task<ActionResult<AffiliateLinkDto>> GetByShortCode([FromRoute] string shortCode)
    {
        if (string.IsNullOrWhiteSpace(shortCode))
            return BadRequest(new { message = "shortCode is required." });

        var entity = await _db.AffiliateLinks.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ShortCode == shortCode.Trim());

        if (entity == null)
            return NotFound(new { message = "Affiliate link not found." });

        return Ok(new AffiliateLinkDto(
            entity.Id,
            entity.ProductId,
            entity.OriginalUrl,
            entity.ShortCode,
            entity.ShortUrl
        ));
    }

    private static string GenerateShortCode(int length)
    {
        const string chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);

        var result = new char[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = chars[bytes[i] % chars.Length];
        }

        return new string(result);
    }
}
