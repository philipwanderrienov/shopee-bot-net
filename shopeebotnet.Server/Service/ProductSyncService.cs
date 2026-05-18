using Microsoft.EntityFrameworkCore;
using shopeebotnet.Server.Configs;
using shopeebotnet.Server.DbContext;
using shopeebotnet.Server.Models;

namespace shopeebotnet.Server.Service;

public sealed class ProductSyncService : IProductSyncService
{
    private readonly ShopeeAffiliateContext _db;

    public ProductSyncService(ShopeeAffiliateContext db)
    {
        _db = db;
    }

    public async Task<ProductSyncResult> SyncOnceAsync(ProductSyncOptions options, CancellationToken cancellationToken)
    {
        var networkName = (options.NetworkName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(networkName))
        {
            return new ProductSyncResult
            {
                Ran = true,
                Succeeded = false,
                Message = "Missing ProductSyncOptions.NetworkName."
            };
        }

        // Credentials check (we don't have decrypt/known auth scheme yet, but we can still decide whether to proceed)
        var hasCredentials = await _db.AffiliateCredentials
            .AsNoTracking()
            .AnyAsync(
                x => x.OwnerUserId != Guid.Empty && x.NetworkName == networkName && x.ApiKeyEncrypted != null && x.ApiKeyEncrypted.Length > 0,
                cancellationToken
            );

        if (!hasCredentials)
        {
            return new ProductSyncResult
            {
                Ran = true,
                Succeeded = false,
                Message = $"No affiliate credentials configured in DB for network_name='{networkName}'. (Waiting for Payment Setting / credentials upload.)"
            };
        }

        // Endpoint mapping/auth is pending (we need exact REST v2 headers/body params).
        // For now we keep the scaffolding running and return a clear message.
        if (string.IsNullOrWhiteSpace(options.ProductListEndpointUrl))
        {
            return new ProductSyncResult
            {
                Ran = true,
                Succeeded = false,
                Message = "ProductListEndpointUrl is not configured. Skipping fetch (scaffolding only)."
            };
        }

        // Future: implement REST v2 request + parsing here once endpoint/auth scheme is known.
        return new ProductSyncResult
        {
            Ran = true,
            Succeeded = false,
            Message = "Product feed fetch is not implemented yet (endpoint/auth mapping pending). Upsert scaffolding is ready."
        };
    }

    // Upsert helper (ready for when fetcher is implemented)
    private async Task UpsertProductsAsync(
        IReadOnlyList<ProductModel> incoming,
        CancellationToken cancellationToken
    )
    {
        if (incoming.Count == 0) return;

        var platformIds = incoming.Select(p => p.ProductIdOnPlatform).ToHashSet(StringComparer.Ordinal);

        var existing = await _db.Products
            .Where(p => platformIds.Contains(p.ProductIdOnPlatform))
            .ToListAsync(cancellationToken);

        var existingByPlatformId = existing.ToDictionary(p => p.ProductIdOnPlatform, StringComparer.Ordinal);

        var now = DateTime.UtcNow;

        foreach (var product in incoming)
        {
            if (existingByPlatformId.TryGetValue(product.ProductIdOnPlatform, out var current))
            {
                // Update mutable fields
                current.Name = product.Name;
                current.Price = product.Price;
                current.OriginalPrice = product.OriginalPrice;
                current.CommissionRate = product.CommissionRate;
                current.Category = product.Category;
                current.ReviewCount = product.ReviewCount;
                current.Rating = product.Rating;
                current.SalesVolume = product.SalesVolume;
                current.ImageUrl = product.ImageUrl;
                current.DataSource = product.DataSource;

                current.UpdatedAt = now;
            }
            else
            {
                // Insert new row
                product.Id = Guid.NewGuid();
                product.CreatedAt = now;
                product.UpdatedAt = now;

                _db.Products.Add(product);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
