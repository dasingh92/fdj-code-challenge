using System;
using System.Collections.Concurrent;
using FdjCodeChallenge.Api.Models;
using Microsoft.Extensions.Logging;
namespace FdjCodeChallenge.Api.Database;

public partial class MyDummyDatabase(ILogger<MyDummyDatabase> logger)
{
    private readonly ILogger<MyDummyDatabase> _logger = logger;

    private readonly ConcurrentDictionary<long, FixturePayload> _fixtures = new();
    private readonly ConcurrentDictionary<(long FixtureId, long CustomerId, string OutcomeKey, decimal Stake, decimal Odds), BetPlacedPayload> _placedBets = new();

    public IReadOnlyCollection<FixturePayload> Fixtures => [.. _fixtures.Values];
    public IReadOnlyCollection<BetPlacedPayload> PlacedBets => [.. _placedBets.Values];
    public void AddFixture(FixturePayload fixture)
    {
        if (_fixtures.TryAdd(fixture.Id, fixture))
        {
            LogFixtureAdded(fixture.Id);
        }
        else
        {
            LogFixtureAlreadyExists(fixture.Id);
        }
    }

    public void AddBet(BetPlacedPayload bet)
    {
        var key = (bet.FixtureId, bet.CustomerId, bet.OutcomeKey, bet.Stake, bet.Odds);
        if(_placedBets.TryAdd(key, bet))
        {
            LogBetAdded(bet.FixtureId, bet.OutcomeKey, bet.Stake, bet.Odds);
        }
        else
        {
            LogBetAlreadyExists(bet.FixtureId, bet.OutcomeKey, bet.Stake, bet.Odds);
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


    [LoggerMessage(Level = LogLevel.Debug, Message = "Fixture with id {FixtureId} added to database")]
    private partial void LogFixtureAdded(long fixtureId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Fixture with id {FixtureId} already exists in database")]
    private partial void LogFixtureAlreadyExists(long fixtureId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Bet on fixture {FixtureId} for outcome {OutcomeKey} with stake {Stake} and odds {Odds} already exists in database")]
    private partial void LogBetAlreadyExists(long fixtureId, string outcomeKey, decimal stake, decimal odds);

    [LoggerMessage(Level = LogLevel.Information, Message = "Bet placed on fixture {FixtureId} for outcome {OutcomeKey} with stake {Stake} and odds {Odds} added to database")]
    private partial void LogBetAdded(long fixtureId, string outcomeKey, decimal stake, decimal odds);
}
