using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using shopeebotnet.Server.Configs;
using shopeebotnet.Server.Service;

namespace shopeebotnet.Server.Controllers;

[ApiController]
[Route("api/products/sync")]
public sealed class ProductsSyncController : ControllerBase
{
    private readonly IProductSyncService _syncService;
    private readonly IOptions<ProductSyncOptions> _options;

    public ProductsSyncController(
        IProductSyncService syncService,
        IOptions<ProductSyncOptions> options
    )
    {
        _syncService = syncService;
        _options = options;
    }

    public record InjectProductsRequest(
        string? ProductListEndpointUrl,
        string? NetworkName
    );

    [Authorize(Roles = "admin")]
    [HttpPost("inject")]
    public async Task<ActionResult<ProductSyncResult>> Inject(
        [FromBody] InjectProductsRequest request,
        CancellationToken cancellationToken
    )
    {
        var opt = _options.Value;

        if (!string.IsNullOrWhiteSpace(request.ProductListEndpointUrl))
            opt.ProductListEndpointUrl = request.ProductListEndpointUrl.Trim();

        if (!string.IsNullOrWhiteSpace(request.NetworkName))
            opt.NetworkName = request.NetworkName.Trim();

        var result = await _syncService.SyncOnceAsync(opt, cancellationToken);
        return Ok(result);
    }
}
