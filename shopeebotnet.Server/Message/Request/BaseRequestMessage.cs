using System;

namespace shopeebotnet.Server.Message.Request;

public class BaseRequestMessage
{
    public int Id { get; set; }
    public string? Date { get; set; }
    public string? Model { get; set; }
    public string? Param { get; set; }
    public string? Token { get; set; }
    public string? UserLogin { get; set; }
    public string? HostName { get; set; }
}
