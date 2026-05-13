using System;

namespace shopeebotnet.Server.Models;

public class DailyRecommendationModel
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }
    public double Score { get; set; }
    public DateOnly RecommendationDate { get; set; }

    public string? WeightBreakdown { get; set; } // store JSONB as raw string for now

    public DateTime CreatedAt { get; set; }
}
