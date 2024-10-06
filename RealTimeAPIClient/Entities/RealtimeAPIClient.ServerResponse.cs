namespace Entitites.RealTimeVoiceAPI;

// ServerResponse and FunctionCall classes to model the expected JSON structure
public sealed class ServerResponse
{
    public FunctionCall FunctionCall { get; set; }
}