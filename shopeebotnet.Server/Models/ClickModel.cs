using System;

namespace shopeebotnet.Server.Models;

public class ClickModel
{
    public Guid Id { get; set; }

    public Guid LinkId { get; set; }
    public Guid ProductId { get; set; }

    public DateTime Timestamp { get; set; }
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
    public string? TrafficSource { get; set; }

    public bool Converted { get; set; }
}
