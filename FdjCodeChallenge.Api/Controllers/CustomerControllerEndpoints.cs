using System.Diagnostics;
using System.Text.Json;
using FdjCodeChallenge.Api.Database;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FdjCodeChallenge.Api.Controllers;

public static class CustomerControllerEndpoints
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private static Dictionary<long, CustomerDetailsResponse> _customerDetailsCache = new();

    public static void MapCustomerControllerEndpoints(this WebApplication app)
    {
        app.MapGet("/customer/{customerId}/stats", GetCustomerStats)
            .WithName("GetCustomerStats")
            .WithTags("Customer")
            .WithDescription("Retrieves statistics for a given customer, including name and total potential payout.")
            .Produces<CustomerStatsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetCustomerStats(long customerId, HttpContext context, HttpClient httpClient, IConfiguration configuration, MyDummyDatabase database, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("CustomerControllerEndpoints");
        var stopwatch = Stopwatch.StartNew();
        var checkIfCustomerExists = database.PlacedBets.Any(bet => bet.CustomerId == customerId);
        if (!checkIfCustomerExists)
        {
            return Results.NotFound();
        }
        // In production this would be done in a separate service class, but for the sake of this challenge we can do it here in the controller.
        var customerDetails = await GetCustomerDetails(customerId, httpClient, configuration, logger);
        if (customerDetails is null)
        {
            return Results.NotFound();
        }
        var customerName = customerDetails.CustomerName;
        var totalStandToWin = database.PlacedBets
            .Where(bet => bet.CustomerId == customerId)
            .Sum(bet => bet.PotentialPayout);
#pragma warning disable CA1873 // Avoid potentially expensive logging
        logger.LogInformation("Retrieved stats for customer {CustomerId} in {ElapsedMilliseconds} ms", customerId, stopwatch.ElapsedMilliseconds);
#pragma warning restore CA1873 // Avoid potentially expensive logging
        return Results.Ok(new CustomerStatsResponse(customerId, customerName, totalStandToWin, stopwatch.ElapsedMilliseconds));
    }

    private static async Task<CustomerDetailsResponse?> GetCustomerDetails(long customerId, HttpClient httpClient, IConfiguration configuration, ILogger logger)
    {
        if (_customerDetailsCache.TryGetValue(customerId, out var cachedCustomerDetails))
        {
            return cachedCustomerDetails;
        }
        var customerNameEndpoint = configuration.GetValue<string>("CustomerDetailsApiEndpoint") ?? throw new NullReferenceException("CustomerDetailsApiEndpoint configuration value is missing");
        var candidateId = configuration.GetValue<string>("CandidateId") ?? throw new NullReferenceException("CandidateId configuration value is missing");
        httpClient.BaseAddress = new Uri(customerNameEndpoint);
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, $"customer?customerId={customerId}&candidateId={candidateId}");
        var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
        if (!httpResponseMessage.IsSuccessStatusCode)
        {
            return null;
        }
        var customerDetails = JsonSerializer.Deserialize<CustomerDetailsResponse>(await httpResponseMessage.Content.ReadAsStringAsync(), _jsonSerializerOptions);
        if (customerDetails is null)
            return null;
        _customerDetailsCache.TryAdd(customerId, customerDetails);
        return customerDetails;
    }

    public record CustomerStatsResponse(long CustomerId, string Name, decimal TotalStandToWin, long millisecondsToRetrieve);
}

internal record CustomerDetailsResponse
{
    public long Id { get; init; }
    public required string CustomerName { get; init; }
}