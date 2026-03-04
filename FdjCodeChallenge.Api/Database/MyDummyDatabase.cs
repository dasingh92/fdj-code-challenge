using System.Collections.Concurrent;
using FdjCodeChallenge.Api.Models;
namespace FdjCodeChallenge.Api.Database;

public partial class MyDummyDatabase(ILogger<MyDummyDatabase> logger)
{
    private readonly ILogger<MyDummyDatabase> _logger = logger;

    private readonly ConcurrentDictionary<long, FixturePayload> _fixtures = new();

    // Not sure if this is necessary, as we could just save the totalStandToWin for each customer as a calculated runtime value. 
    // The API contract only demands the total potential payout for a given customer, so we don't necessarily need to store all the bets, but it could be useful for future features and it's not too expensive to store them in memory for the sake of this challenge.
    private readonly ConcurrentDictionary<BetPlacedPayload, BetPlacedPayload> _placedBets = new();

    public IReadOnlyCollection<FixturePayload> Fixtures => [.. _fixtures.Values];
    public IReadOnlyCollection<BetPlacedPayload> PlacedBets => [.. _placedBets.Values];
    public void AddFixture(FixturePayload fixture)
    {
        if (_fixtures.TryAdd(fixture.Id, fixture))
        {
            LogFixtureAdded(_logger, fixture.Id);
        }
        else
        {
            LogFixtureAlreadyExists(_logger, fixture.Id);
        }
    }

    public void AddBet(BetPlacedPayload bet)
    {
        var key = bet;
        if(_placedBets.TryAdd(key, bet))
        {
            LogBetAdded(_logger, bet.FixtureId, bet.OutcomeKey, bet.Stake, bet.Odds, bet.StandToWin);
        }
        else
        {
            LogBetAlreadyExists(_logger, bet.FixtureId, bet.OutcomeKey, bet.Stake, bet.Odds);
        }
    }

    public FixturePayload? GetFixture(long fixtureId)
    {
        _fixtures.TryGetValue(fixtureId, out var fixture);
        return fixture;
    }

    public void Clear()
    {
        _fixtures.Clear();
        _placedBets.Clear();
    }

    // In production this should be a separate service class or static helper class, but for the sake of this challenge we can keep it here in the database class.
    [LoggerMessage( Level = LogLevel.Debug, Message = "Fixture with id {FixtureId} added to database")]
    private static partial void LogFixtureAdded(ILogger logger, long fixtureId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Fixture with id {FixtureId} already exists in database")]
    private static partial void LogFixtureAlreadyExists(ILogger logger, long fixtureId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Bet on fixture {FixtureId} for outcome {OutcomeKey} with stake {Stake} and odds {Odds} already exists in database")]
    private static partial void LogBetAlreadyExists(ILogger logger, long fixtureId, string outcomeKey, decimal stake, decimal odds);

    [LoggerMessage(Level = LogLevel.Information, Message = "Bet placed on fixture {FixtureId} for outcome {OutcomeKey} with stake {Stake} and odds {Odds} (stand to win: {StandToWin}) added to database")]
    private static partial void LogBetAdded(ILogger logger, long fixtureId, string outcomeKey, decimal stake, decimal odds, decimal standToWin);
}
