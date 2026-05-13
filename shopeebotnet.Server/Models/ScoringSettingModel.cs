using System;

namespace shopeebotnet.Server.Models;

public class ScoringSettingModel
{
    public Guid Id { get; set; }

    public string Weights { get; set; } = string.Empty; // JSONB as string for now

    public DateOnly? ActiveFrom { get; set; }
    public DateOnly? ActiveTo { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
