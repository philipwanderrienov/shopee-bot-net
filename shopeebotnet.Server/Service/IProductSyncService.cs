using shopeebotnet.Server.Configs;

namespace shopeebotnet.Server.Service;

public interface IProductSyncService
{
    Task<ProductSyncResult> SyncOnceAsync(ProductSyncOptions options, CancellationToken cancellationToken);
}
