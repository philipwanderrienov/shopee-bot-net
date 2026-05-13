using System;

namespace shopeebotnet.Server.Message.Response;

public class BaseResponseMessage
{
    public bool success { get; set; }
    public bool exception { get; set; } = false;
    public string? message { get; set; }
    public object? result { get; set; }
}
