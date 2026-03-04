using System.Text.Json;

namespace FdjCodeChallenge.Api.Models;

public record BaseMessage
{
    public MessageType Type { get; init; }
    public JsonElement Payload { get; init; }
    public DateTime Timestamp { get; init; }
}

public enum MessageType
{
    Fixture,
    BetPlaced,
    EndOfFeed
}
