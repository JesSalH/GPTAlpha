using Entitites.RealTimeVoiceAPI;
using NAudio.Wave;
using NWaveApp;
using Serilog;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;


namespace RealTimeVoiceAPI;

public partial class RealtimeAPIClient
{
    private readonly ILogger _logger;
    private ClientWebSocket _socket;
    private readonly AudioManager _audioManager;

    public RealtimeAPIClient(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _audioManager = new AudioManager(_logger);
    }

    public async Task ConnectAsync(string uri, string apiKey)
    {
        _socket = new ClientWebSocket(); // Initialize the socket here

        try
        {
            // Set headers
            _socket.Options.SetRequestHeader("Authorization", $"Bearer {apiKey}");
            _socket.Options.SetRequestHeader("OpenAI-Beta", "realtime=v1");

            var uriWithQuery = $"{uri}?model=gpt-4o-realtime-preview-2024-10-01";
            await _socket.ConnectAsync(new Uri(uriWithQuery), CancellationToken.None);
            _logger.Information("Connected to WebSocket server.");

            // Start sending and receiving audio
            await SendAndReceiveAudio(_socket);
        }
        catch (WebSocketException ex)
        {
            _logger.Error($"WebSocket connection failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error: {ex.Message}");
        }
    }

    private async Task ReceiveMessagesAsync(ClientWebSocket socket)
    {
        var buffer = new byte[1024 * 4];
        while (socket.State == WebSocketState.Open)
        {
            try
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                }
                else
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _logger.Information($"Text Response: {message}");

                    if (IsAudioResponse(message))
                    {
                        _logger.Information("Processing audio response...");
                        HandleAudioResponse(message);
                    }
                    else if (IsSessionCreated(message))
                    {
                        _logger.Information("Session created successfully.");
                    }
                    else
                    {
                        _logger.Information("Processing text response...");
                        await HandleTextResponse(message);
                    }

                    // Check if the message indicates the end of the conversation
                    if (IsEndOfConversation(message))
                    {
                        _logger.Information("End of conversation detected.");
                        break;
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.Error($"Error parsing JSON: {ex.Message}");
            }
            catch (WebSocketException ex)
            {
                _logger.Error($"WebSocket error: {ex.Message}");
                if (socket.State != WebSocketState.Open)
                {
                    _logger.Information("Attempting to reconnect...");
                    //await ReconnectAsync();
                    break;
                }
            }
            catch (IOException ex)
            {
                _logger.Error($"IO error: {ex.Message}");
                if (socket.State != WebSocketState.Open)
                {
                    _logger.Information("Attempting to reconnect...");
                    //await ReconnectAsync();
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Unexpected error: {ex.Message}");
            }
        }
    }


    private bool IsEndOfConversation(string message)
    {
        try
        {
            var response = JsonSerializer.Deserialize<ServerResponse>(message);
            return response?.FunctionCall?.Name == "end_conversation";
        }
        catch (JsonException ex)
        {
            _logger.Error($"Error parsing JSON: {ex.Message}");
            return false;
        }
    }

    private bool IsSessionCreated(string message)
    {
        try
        {
            var jsonDocument = JsonDocument.Parse(message);
            if (jsonDocument.RootElement.TryGetProperty("type", out JsonElement typeElement) &&
                typeElement.GetString() == "session.created")
            {
                return true;
            }
        }
        catch (JsonException ex)
        {
            _logger.Error($"Error parsing JSON: {ex.Message}");
        }

        return false;
    }

    private bool IsAudioResponse(string message)
    {
        try
        {
            var jsonDocument = JsonDocument.Parse(message);
            if (jsonDocument.RootElement.TryGetProperty("type", out JsonElement typeElement))
            {
                // Check if the type is "audio"
                if (typeElement.GetString() == "audio")
                {
                    return true;
                }

                // Check if the type is "session.created" and it contains "audio" in modalities
                if (typeElement.GetString() == "session.created" &&
                    jsonDocument.RootElement.TryGetProperty("session", out JsonElement sessionElement) &&
                    sessionElement.TryGetProperty("modalities", out JsonElement modalitiesElement) &&
                    modalitiesElement.EnumerateArray().Any(modality => modality.GetString() == "audio"))
                {
                    return false;
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.Error($"Error parsing JSON: {ex.Message}");
        }

        return false;
    }

    private void HandleAudioResponse(string message)
    {
        try
        {
            // Extract the base64 audio data from the response
            var audioData = ExtractAudioData(message);

            // Convert the base64 string to byte array
            byte[] audioBytes = Convert.FromBase64String(audioData);

            // Play the audio
            _audioManager.PlayAudio(audioBytes, audioBytes.Length);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error processing audio response: {ex.Message}");
        }
    }

    private string ExtractAudioData(string message)
    {
        try
        {
            var response = JsonSerializer.Deserialize<AudioResponse>(message);
            return response?.Audio?.Content; // Adjust this according to your response structure
        }
        catch (JsonException ex)
        {
            _logger.Error($"Error extracting audio data: {ex.Message}");
            return string.Empty;
        }
    }

    private async Task HandleTextResponse(string message)
    {
        try
        {
            _logger.Information($"Text Response: {message}");

            // Check if the response contains a function call
            var response = JsonSerializer.Deserialize<ServerResponse>(message);
            if (response?.FunctionCall != null)
            {
                await ProcessFunctionCall(response.FunctionCall);
            }
            else
            {
                _logger.Information("No function call found in the response.");
            }
        }
        catch (JsonException ex)
        {
            _logger.Error($"Error parsing text response: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error processing text response: {ex.Message}");
        }
    }

    private async Task SendAudioAsync(ClientWebSocket socket)
    {
        try
        {
            using (var waveIn = new WaveInEvent())
            {
                waveIn.WaveFormat = new WaveFormat(24000, 16, 1);
                bool isRecording = true;

                waveIn.DataAvailable += async (sender, e) =>
                {
                    if (!isRecording) return;

                    try
                    {
                        var floatData = ByteToFloat(e.Buffer);
                        var pcmData = FloatTo16BitPCM(floatData);
                        string base64Audio = Convert.ToBase64String(pcmData);

                        var eventPayload = new
                        {
                            type = "conversation.item.create",
                            item = new
                            {
                                type = "message",
                                role = "user",
                                content = new[] { new { type = "input_audio", audio = base64Audio } }
                            }
                        };

                        await socket.SendAsync(
                            new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(eventPayload))),
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None
                        );
                    }
                    catch (WebSocketException ex)
                    {
                        _logger.Error($"Error sending audio data: {ex.Message}");
                    }
                };

                _audioManager.RecordVoice();               
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error capturing or sending audio: {ex.Message}");
        }
    }    

    private async Task SendAndReceiveAudio(ClientWebSocket socket)
    {
        // First, receive the initial context message from the API
        //await ReceiveMessagesAsync(socket);

        while (socket.State == WebSocketState.Open)
        {
            Console.WriteLine("Do you want to send a voice message? (yes/no)");
            string userInput = Console.ReadLine()?.Trim().ToLower();

            if (userInput == "yes")
            {
                await SendAudioAsync(socket);

                // Wait for the response
                await ReceiveMessagesAsync(socket);
            }
            else if (userInput == "no")
            {
                Console.WriteLine("No voice message will be sent.");
                break;
            }
            else
            {
                Console.WriteLine("Invalid input. Please type 'yes' or 'no'.");
            }
        }
    }

    // Helper method to convert byte[] to float[]
    private float[] ByteToFloat(byte[] byteArray)
    {
        float[] floatArray = new float[byteArray.Length / 2];
        for (int i = 0; i < floatArray.Length; i++)
        {
            short sample = (short)(byteArray[i * 2] | (byteArray[i * 2 + 1] << 8));
            floatArray[i] = sample / 32768f; // Scale to -1.0 to 1.0
        }
        return floatArray;
    }

    private async Task ProcessFunctionCall(FunctionCall functionCall)
    {
        try
        {
            // Here you would invoke the appropriate function based on the functionCall details
            // For demonstration, we'll just log the function call
            _logger.Information($"Function Call: {functionCall.Name} with parameters: {JsonSerializer.Serialize(functionCall.Parameters)}");

            // After processing, if you need to send a response back, you can do so
            var responseMessage = JsonSerializer.Serialize(new
            {
                response = "Function call processed successfully."
            });

            await SendTextResponse(responseMessage, _socket);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error processing function call: {ex.Message}");
        }
    }

    private async Task SendTextResponse(string message, ClientWebSocket socket)
    {
        try
        {
            // Construct the payload
            var responsePayload = new
            {
                type = "conversation.item.create",
                item = new
                {
                    type = "message",
                    role = "system", // Adjust the role as needed
                    content = message
                }
            };

            // Send the response back to the server
            var payloadJson = JsonSerializer.Serialize(responsePayload);
            var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
            await socket.SendAsync(new ArraySegment<byte>(payloadBytes), WebSocketMessageType.Text, true, CancellationToken.None);

            _logger.Information($"Response sent: {message}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error sending response: {ex.Message}");
        }
    }

    // Updated FloatTo16BitPCM method
    public static byte[] FloatTo16BitPCM(float[] float32Array)
    {
        byte[] buffer = new byte[float32Array.Length * 2];
        int offset = 0;
        for (int i = 0; i < float32Array.Length; i++, offset += 2)
        {
            // Clamp value to the range of -1 to 1
            float s = Math.Max(-1f, Math.Min(1f, float32Array[i]));
            short sample = (short)(s < 0 ? s * 0x8000 : s * 0x7FFF);

            // Convert short to byte array
            buffer[offset] = (byte)(sample & 0xFF);          // Low byte
            buffer[offset + 1] = (byte)((sample >> 8) & 0xFF); // High byte
        }
        return buffer;
    }

    private void HandleError(string jsonData)
    {
        try
        {
            var errorEvent = JsonSerializer.Deserialize<ErrorEvent>(jsonData);
            _logger.Error($"Error Type: {errorEvent.Type}, Code: {errorEvent.Code}, Message: {errorEvent.Message}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error parsing error event: {ex.Message}");
        }
    }

}