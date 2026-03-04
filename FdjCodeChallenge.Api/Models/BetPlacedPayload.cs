namespace FdjCodeChallenge.Api.Models;

public record BetPlacedPayload
{
    public long FixtureId { get; init; }
    public required string OutcomeKey { get; init; }
    public long CustomerId { get; init; }
    public decimal Odds { get; init; }
    public decimal Stake { get; init; }

    // Payout = Stake × Odds (total returned by house)
    // StandToWin = Payout - Stake = net profit/gain if bet wins
    public decimal StandToWin => Stake * Odds - Stake;
}
