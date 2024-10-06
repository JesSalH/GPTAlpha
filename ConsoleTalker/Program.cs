using ChatCompletions;
using Microsoft.Extensions.Configuration;
using Serilog;
namespace ConsoleTalker;

internal class Program
{

    private static ChatCompletionsClient _chatCompletionsClient;

    static async Task Main(string[] args)
    {
        Console.WriteLine("Welcome to your shopping assistant!");

        // Set up Serilog for logging
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        // Build configuration to access user secrets
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .Build();

        // Retrieve the API key from user secrets
        string apiKey = configuration["ChatCompletions:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine("API key is missing. Please set it in the user secrets.");
            return;
        }

        // Instantiate the HttpClientFactory
        var httpClientFactory = new HttpClientFactory();

        // Create the ChatCompletionsClient using the factory
        _chatCompletionsClient = httpClientFactory.CreateClient(apiKey);

        // Start the conversation loop
        await StartConversationLoop();
    }

    private static async Task StartConversationLoop()
    {
        while (true)
        {
            Console.WriteLine("Enter your message for the role 'user':");
            string userMessage = Console.ReadLine();

            Console.WriteLine("Enter your message for the role 'system' (or leave empty):");
            string systemMessage = Console.ReadLine();

            //Console.WriteLine("Enter your message for the role 'assistant' (or leave empty):");
            string assistantMessage = Console.ReadLine();

            // Send the message to the Chat Completions API
            string response = await _chatCompletionsClient.SendMessageToChatCompletions(userMessage??"", systemMessage, assistantMessage);

            // Display the response
            Console.WriteLine("Response from ChatCompletions API: " + response);

            // Ask if the user wants to continue or exit
            Console.WriteLine("Do you want to ask another question? (yes to continue, any other key to exit):");
            string continueResponse = Console.ReadLine();

            if (!continueResponse.Equals("yes", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
        }
    }
}
