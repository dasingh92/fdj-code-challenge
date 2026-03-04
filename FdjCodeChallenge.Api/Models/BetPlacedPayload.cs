namespace FdjCodeChallenge.Api.Models;

public record BetPlacedPayload
{
    public long FixtureId { get; init; }
    public required string OutcomeKey { get; init; }
    public long CustomerId { get; init; }
    public decimal Odds { get; init; }
    public decimal Stake { get; init; }

    public decimal PotentialPayout => Stake * Odds;
}
