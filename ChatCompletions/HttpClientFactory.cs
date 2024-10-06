using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Net.Http;

namespace ChatCompletions;
public class HttpClientFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    public HttpClientFactory()
    {
        // Set up the service collection
        var services = new ServiceCollection();

        // Register the HttpClient
        services.AddHttpClient<ChatCompletionsClient>(client =>
        {
            // You can configure your HttpClient here
            client.BaseAddress = new Uri("https://api.openai.com/");
            client.DefaultRequestHeaders.Add("User-Agent", "ChatGPT-Console-App");
        });

        // Build the service provider
        _serviceProvider = services.BuildServiceProvider();

        // Create a logger instance
        _logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();
    }

    public ChatCompletionsClient CreateClient(string apiKey)
    {
        var httpClient = _serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("ChatCompletionsClient");
        return new ChatCompletionsClient(httpClient, apiKey, _logger);
    }
}
