namespace Entitites.RealTimeVoiceAPI;

// Example of the ErrorEvent class to model error messages
public sealed class ErrorEvent
{
    public string Type { get; set; }
    public string Code { get; set; }
    public string Message { get; set; }
    public string Param { get; set; }
    public string EventId { get; set; }
}

