using System;

namespace jitu_dashboard.Server.Message.Request;

public class RequestModel
{
    public string? AuthKey { get; set; }
    public string? RequestId { get; set; }
    public string? TxDate { get; set; }
    public string? TxHour { get; set; }
    public string? UserGtw { get; set; }
    public string? ChannelId { get; set; }
    public string? Date { get; set; }
    public int Id { get; set; }
    // public EnumTransactionStatus? TransactionStatus { get; set; }
    public string? Model { get; set; }
    public string? Param { get; set; }
    public string? Token { get; set; }
    public string? UserLogin { get; set; }
    public string? HostName { get; set; }
}
