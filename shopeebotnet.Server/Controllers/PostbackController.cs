

using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shopeebotnet.Server.DbContext;
using shopeebotnet.Server.Models;

namespace shopeebotnet.Server.Controllers;

[ApiController]
[Route("api/postback")]
public class PostbackController : ControllerBase
{
    private readonly ShopeeAffiliateContext _db;
    private readonly IConfiguration _configuration;

    public PostbackController(ShopeeAffiliateContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public record ConversionPostbackRequest(
        Guid ClickId,
        string OrderId,
        decimal Commission,
        string? Status // "approved" or "rejected"
    );

    [HttpPost("conversion")]
    public async Task<IActionResult> PostConversion()
    {
        var secret = _configuration["Postback:Secret"];
        if (string.IsNullOrWhiteSpace(secret))
            return StatusCode(500, new { message = "Postback secret is not configured." });

        var signatureHeaderNames = new[]
        {
            "X-Postback-Signature",
            "X-Signature",
            "X-Hub-Signature"
        };

        string? signatureHeaderValue = null;
        foreach (var headerName in signatureHeaderNames)
        {
            if (Request.Headers.TryGetValue(headerName, out var candidate) &&
                !string.IsNullOrWhiteSpace(candidate))
            {
                signatureHeaderValue = candidate.ToString();
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(signatureHeaderValue))
        {
            return Unauthorized(new
            {
                message = "Missing postback signature header. Tried: X-Postback-Signature, X-Signature, X-Hub-Signature."
            });
        }

        // Compute signature over raw body bytes (stable for the caller)
        Request.EnableBuffering();
        string rawBody;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
        {
            rawBody = await reader.ReadToEndAsync();
        }
        Request.Body.Position = 0;

        var expectedSignature = ComputeHmacSha256Hex(secret, rawBody);
        if (!SecureEquals(signatureHeaderValue!, expectedSignature))
            return Unauthorized(new { message = "Invalid postback signature." });

        ConversionPostbackRequest? payload;
        try
        {
            payload = await System.Text.Json.JsonSerializer.DeserializeAsync<ConversionPostbackRequest>(
                Request.Body,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
        }
        catch
        {
            return BadRequest(new { message = "Invalid JSON payload." });
        }

        if (payload == null)
            return BadRequest(new { message = "Missing payload." });

        if (payload.ClickId == Guid.Empty)
            return BadRequest(new { message = "ClickId is required." });

        if (string.IsNullOrWhiteSpace(payload.OrderId))
            return BadRequest(new { message = "OrderId is required." });

        var orderId = payload.OrderId.Trim();

        var click = await _db.Clicks.FirstOrDefaultAsync(x => x.Id == payload.ClickId);
        if (click == null)
            return NotFound(new { message = "Click not found." });

        var now = DateTime.UtcNow;
        var status = ParseStatus(payload.Status);
        var statusText = status.ToString(); // "pending" | "approved" | "rejected"

        // Idempotency: if this postback is retried, avoid double-inserting conversions.
        // Instead of using EF to insert/update the enum (which is currently failing),
        // we do explicit SQL casts to the postgres enum type.
        var existingConversionId = await _db.Conversions
            .AsNoTracking()
            .Where(x => x.ClickId == payload.ClickId && x.OrderId == orderId)
            .Select(x => x.Id)
            .FirstOrDefaultAsync();

        click.Converted = true;

        if (existingConversionId == Guid.Empty)
        {
            var conversionId = Guid.NewGuid();

            await _db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO conversions (id, click_id, order_id, commission, status, recorded_at)
                VALUES ({conversionId}, {payload.ClickId}, {orderId}, {payload.Commission}, {statusText}::conversion_status, {now});
            ");
        }
        else
        {
            await _db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE conversions
                SET commission = {payload.Commission},
                    status = {statusText}::conversion_status,
                    recorded_at = {now}
                WHERE click_id = {payload.ClickId} AND order_id = {orderId};
            ");
        }

        await _db.SaveChangesAsync();

        return Ok(new { message = "Postback recorded." });
    }

    private static ConversionStatusModel ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return ConversionStatusModel.approved;

        var normalized = status.Trim().ToLowerInvariant();
        return normalized switch
        {
            "approved" => ConversionStatusModel.approved,
            "rejected" => ConversionStatusModel.rejected,
            "pending" => ConversionStatusModel.pending,
            _ => ConversionStatusModel.approved
        };
    }

    private static string ComputeHmacSha256Hex(string secret, string rawBody)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var bodyBytes = Encoding.UTF8.GetBytes(rawBody);

        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(bodyBytes);

        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    private static bool SecureEquals(string a, string b)
    {
        // constant-time compare
        var aBytes = Encoding.UTF8.GetBytes(a ?? string.Empty);
        var bBytes = Encoding.UTF8.GetBytes(b ?? string.Empty);
        if (aBytes.Length != bBytes.Length) return false;

        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }
}
