using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using FdjCodeChallenge.Api.Database;

namespace FdjCodeChallenge.Api.Services;

public class WebSocketConsumerService(ILogger<WebSocketConsumerService> logger, IConfiguration configuration, MyDummyDatabase database) : BackgroundService
{
    private readonly ILogger<WebSocketConsumerService> _logger = logger;
    private readonly MyDummyDatabase _database = database;
    private readonly string _webSocketUrl = configuration.GetValue<string>("WebSocketEndpoint") ?? throw new NullReferenceException("WebSocketEndpoint configuration value is missing");
    private readonly string CandidateId = configuration.GetValue<string>("CandidateId") ?? throw new NullReferenceException("CandidateId configuration value is missing");

    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var ws = new ClientWebSocket();
        ws.Options.KeepAliveInterval = WebSocket.DefaultKeepAliveInterval;
        _logger.LogInformation("Connecting to WebSocket at {WebSocketUrl}", _webSocketUrl);
        var webSocketUrlWithCandidateId = $"{_webSocketUrl}?candidateId={CandidateId}";
        try
        {
            await ws.ConnectAsync(new Uri(webSocketUrlWithCandidateId), stoppingToken);

            while (!stoppingToken.IsCancellationRequested && ws.State == WebSocketState.Open)
            {
                await ReadSingleMessageFromWebSocketAsync(ws, stoppingToken);
            }
        }
        catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely
                                                || ex.Message.Contains("close handshake")
                                                || ex.InnerException is IOException)
        {
            _logger.LogWarning(ex, "WebSocket connection lost. Will attempt to reconnect...");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("WebSocket service is stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in WebSocket consumer");
        }
        finally
        {
            if (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)
            {
                _logger.LogInformation("Closing WebSocket connection");
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Service stopping", CancellationToken.None);
            }
        }
        
    }

    private async Task ReadSingleMessageFromWebSocketAsync(ClientWebSocket ws, CancellationToken stoppingToken)
    {
        // Note: In production, we should handle the case where messages are larger than the buffer size and need to be read in multiple chunks. For the sake of this challenge, we can assume that messages will fit in the buffer.
        var buffer = new byte[1024];
        var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), stoppingToken);
        if (result.MessageType == WebSocketMessageType.Close)
        {
            _logger.LogInformation("WebSocket connection closed by server");
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", stoppingToken);
        }
        else if (result.MessageType == WebSocketMessageType.Text)
        {
            // Using UTF-8 as the requirements specify UTF-8 serialized strings.
            var message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
            _logger.LogInformation("Received message: {Message}", message);
            // Process the message as needed
            var baseMessage = JsonSerializer.Deserialize<Models.BaseMessage>(message, _jsonOptions);
            if (baseMessage is not null)
            {
                switch (baseMessage.Type)
                {
                    case Models.MessageType.Fixture:
                        var fixturePayload = baseMessage.Payload.Deserialize<Models.FixturePayload>(_jsonOptions);
                        if (fixturePayload is not null)
                        {
                            _logger.LogInformation("Deserialized FixturePayload with id {FixtureId}", fixturePayload.Id);
                            // Store the fixture in the database
                            _database.AddFixture(fixturePayload);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to deserialize FixturePayload from message: {Message}", message);
                        }
                        break;
                    case Models.MessageType.BetPlaced:
                        var betPlacedPayload = baseMessage.Payload.Deserialize<Models.BetPlacedPayload>(_jsonOptions);
                        if (betPlacedPayload is not null)
                        {
                            _logger.LogInformation("Deserialized BetPlacedPayload for fixture id {FixtureId} and outcome {OutcomeKey}", betPlacedPayload.FixtureId, betPlacedPayload.OutcomeKey);
                            // Store the bet in the database
                            _database.AddBet(betPlacedPayload);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to deserialize BetPlacedPayload from message: {Message}", message);
                        }
                        break;
                    case Models.MessageType.EndOfFeed:
                        _logger.LogInformation("Received EndOfFeed message");
                        // Close the WebSocket connection gracefully.
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "End of feed received", stoppingToken);
                        _database.Clear();
                        break;
                    default:
                        _logger.LogError("Received message with unknown type: {MessageType}", baseMessage.Type);
                        break;
                }
            }
            else
            {
                _logger.LogWarning("Failed to deserialize BaseMessage from message: {Message}", message);
            }
        }
    }
}
