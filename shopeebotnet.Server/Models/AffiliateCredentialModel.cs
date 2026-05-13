using System;

namespace shopeebotnet.Server.Models;

public class AffiliateCredentialModel
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }

    public string NetworkName { get; set; } = string.Empty;
    public byte[] ApiKeyEncrypted { get; set; } = Array.Empty<byte>();
    public byte[]? ApiSecretEncrypted { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
