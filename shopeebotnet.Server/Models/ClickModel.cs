using System;
using System.Net;

namespace shopeebotnet.Server.Models;

public class ClickModel
{
    public Guid Id { get; set; }

    public Guid LinkId { get; set; }
    public Guid ProductId { get; set; }

    public DateTime Timestamp { get; set; }
    public IPAddress? Ip { get; set; }
    public string? UserAgent { get; set; }
    public string? TrafficSource { get; set; }

    public bool Converted { get; set; }
}
