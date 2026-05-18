namespace shopeebotnet.Server.Configs;

public sealed class ProductSyncOptions
{
    public bool Enabled { get; set; } = false;

    // Interval in minutes for periodic sync
    public int IntervalMinutes { get; set; } = 60;

    // Which affiliate network credential to use (e.g. "involve_asia", "access_trade")
    public string NetworkName { get; set; } = "shopee_affiliate";

    // Controls whether the sync should run if credentials exist but API endpoint mapping is not configured
    public bool StrictMode { get; set; } = false;

    // Optional: if you want to call a specific REST v2 endpoint directly (full URL)
    // NOTE: Required later when we have endpoint/header+param mapping from Shopee docs.
    public string? ProductListEndpointUrl { get; set; }

    // Optional: only for manual testing while DB credentials encryption is being finalized.
    // If set, it will be preferred over DB credentials (still best-effort decrypted).
    public string? ApiKeyOverride { get; set; }
    public string? ApiSecretOverride { get; set; }
}
