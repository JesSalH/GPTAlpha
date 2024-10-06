using Newtonsoft.Json;
using Serilog;
using System.Net.Http.Headers;
using System.Text;

namespace ChatCompletions;

public class ChatCompletionsClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger _logger; // Serilog logger

    public ChatCompletionsClient(HttpClient httpClient, string apiKey, ILogger logger)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _logger = logger;
    }

    public async Task<string> SendMessageToChatCompletions(string userMessage, string? systemMessage = null, string? assistantMessage = null )
    {
        var requestBody = new
        {
            model = "gpt-4o-mini", // Use a valid model name
            messages = new[]
            {
                new { role = "user", content = userMessage }, // Proper structure for messages
                new { role = "system", content = systemMessage ?? "" },
                new { role = "assistant", content = assistantMessage ?? "" }
            },
            max_tokens = 200,
            temperature = 1,
            top_p = 1,
            frequency_penalty = 2,
            presence_penalty = -1
            //stop = "\n" // Uncomment if you want to use a stop sequence
        };

        var requestJson = JsonConvert.SerializeObject(requestBody);

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("https://api.openai.com/v1/chat/completions"),
            Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
        };

        // Set the authorization header
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        try
        {
            var response = await _httpClient.SendAsync(request);
            return await HandleResponse(response);
        }
        catch (HttpRequestException ex)
        {
            _logger.Error("HTTP Request Exception: {ExceptionMessage}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error("Unexpected Exception: {ExceptionMessage}", ex.Message);
            throw;
        }
    }

    private async Task<string> HandleResponse(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            dynamic jsonResponse = JsonConvert.DeserializeObject(responseContent);
            //_logger.Information("Response from Chat Completions API: {ResponseContent}", responseContent);
            return jsonResponse.choices[0].message.content; // Adjust this if the structure changes
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.Error("Request failed with status code: {StatusCode}, Response: {ResponseContent}", response.StatusCode, errorContent);
            throw new HttpRequestException($"Request failed with status code: {response.StatusCode}, Response: {errorContent}");
        }
    }
}
