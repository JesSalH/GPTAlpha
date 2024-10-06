using Microsoft.Extensions.Configuration;
using Serilog;

namespace ConsoleVoice;

internal class Program
{
    private static RealtimeAPIClient _realtimeAPIClient;

    static async Task Main(string[] args)
    {
        Console.WriteLine("Console voice master!");

        // Set up Serilog for logging
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        // Build configuration to access user secrets
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .Build();

        // Retrieve the API key from user secrets
        string apiKey = configuration["RealTimeAPI:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine("API key is missing. Please set it in the user secrets.");
            return;
        }

        // Instantiate the RealtimeAPIClient
        _realtimeAPIClient = new RealtimeAPIClient(Log.Logger);

        // Connect to the WebSocket server
        string uri = "wss://api.openai.com/v1/realtime"; // Replace with your actual WebSocket server URI
        await _realtimeAPIClient.ConnectAsync(uri, apiKey);
    }   
}