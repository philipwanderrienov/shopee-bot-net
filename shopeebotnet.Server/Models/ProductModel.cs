using System;

namespace shopeebotnet.Server.Models;

public class ProductModel
{
    public Guid Id { get; set; }

    public string ProductIdOnPlatform { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }
    public decimal OriginalPrice { get; set; }

    public decimal CommissionRate { get; set; }
    public string Category { get; set; } = string.Empty;

    public int ReviewCount { get; set; }
    public decimal Rating { get; set; }
    public int? SalesVolume { get; set; }

    public string? ImageUrl { get; set; }
    public string DataSource { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
