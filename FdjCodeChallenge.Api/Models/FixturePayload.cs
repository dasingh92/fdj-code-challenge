namespace FdjCodeChallenge.Api.Models;

public record FixturePayload
{
    public long Id { get; init; }
    public DateTime StartTime { get; init; }
    public required string Name { get; init; }
    public IReadOnlyList<Outcome> Outcomes { get; init; } = [];
}

public record Outcome
{
    public required string Key { get; init; }
    public required string Name { get; init; }
}
