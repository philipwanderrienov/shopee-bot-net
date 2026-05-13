using System;

namespace shopeebotnet.Server.Models;

public class AffiliateLinkModel
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }

    public string OriginalUrl { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public string ShortUrl { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
