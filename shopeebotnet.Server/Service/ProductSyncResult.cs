namespace shopeebotnet.Server.Service;

public sealed class ProductSyncResult
{
    public bool Ran { get; set; }
    public bool Succeeded { get; set; }

    public string Message { get; set; } = string.Empty;

    public int ProductsUpserted { get; set; }
    public int ProductsSkipped { get; set; }
}
