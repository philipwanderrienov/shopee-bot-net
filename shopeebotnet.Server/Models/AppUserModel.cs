using System;

namespace shopeebotnet.Server.Models;

public class AppUserModel
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public AffiliateRoleModel Role { get; set; } = AffiliateRoleModel.affiliate;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
