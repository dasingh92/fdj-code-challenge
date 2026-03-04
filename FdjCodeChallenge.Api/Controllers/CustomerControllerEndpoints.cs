using System;
using System.Text.Json;
using FdjCodeChallenge.Api.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FdjCodeChallenge.Api.Controllers;

public static class CustomerControllerEndpoints
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public static void MapCustomerControllerEndpoints(this WebApplication app)
    {
        app.MapGet("/customer/{customerId}/stats", GetCustomerStats)
            .WithName("GetCustomerStats")
            .WithTags("Customer")
            .WithDescription("Retrieves statistics for a given customer, including name and total potential payout.")
            .Produces<CustomerStatsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetCustomerStats(long customerId, HttpContext context, HttpClient httpClient, IConfiguration configuration, MyDummyDatabase database)
    {
        var checkIfCustomerExists = database.PlacedBets.Any(bet => bet.CustomerId == customerId);
        if (!checkIfCustomerExists)
        {
            return Results.NotFound();
        }
        // In production this would be done in a separate service class, but for the sake of this challenge we can do it here in the controller.
        var customerNameEndpoint = configuration.GetValue<string>("CustomerDetailsApiEndpoint") ?? throw new NullReferenceException("CustomerDetailsApiEndpoint configuration value is missing");
        var candidateId = configuration.GetValue<string>("CandidateId") ?? throw new NullReferenceException("CandidateId configuration value is missing");
        httpClient.BaseAddress = new Uri(customerNameEndpoint);
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, $"customer?customerId={customerId}&candidateId={candidateId}");
        var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
        if(!httpResponseMessage.IsSuccessStatusCode)
        {
            return Results.NotFound();
        }
        var customerDetails = JsonSerializer.Deserialize<CustomerDetailsResponse>(await httpResponseMessage.Content.ReadAsStringAsync(), _jsonSerializerOptions);
        if (customerDetails is null)        
            return Results.NotFound();
        var customerName = customerDetails.CustomerName;
        var totalStandToWin = database.PlacedBets
            .Where(bet => bet.CustomerId == customerId)
            .Sum(bet => bet.PotentialPayout);
        return httpResponseMessage.IsSuccessStatusCode
            ? Results.Ok(new CustomerStatsResponse(customerId, customerName, totalStandToWin))
            : Results.NotFound();
    }

    public record CustomerStatsResponse(long CustomerId, string Name, decimal TotalStandToWin);
}

internal record CustomerDetailsResponse
{
    public long Id { get; init; }
    public required string CustomerName { get; init; }
}