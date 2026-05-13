using System;

namespace shopeebotnet.Server.Models;

public class ConversionModel
{
    public Guid Id { get; set; }

    public Guid ClickId { get; set; }

    public string OrderId { get; set; } = string.Empty;
    public decimal Commission { get; set; }
    public ConversionStatusModel Status { get; set; } = ConversionStatusModel.pending;

    public DateTime RecordedAt { get; set; }
}
