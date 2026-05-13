using System;

namespace jitu_dashboard.Server.Message.Response;

public class ResponseModel
{
    public bool success { get; set; }
    public bool exception { get; set; } = false;
    public string? code { get; set; }
    public string? message { get; set; }
    public object? result { get; set; }
    // public EnumCode? enumCode { get; set; }
}
