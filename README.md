# FDJ Code Challenge

A real-time betting statistics API built with .NET 10 that consumes WebSocket messages for fixtures and bets, and provides customer statistics via REST API.

## Overview

This application demonstrates a real-time betting system that:
- Consumes fixture and bet placement data from a WebSocket stream
- Stores data in an in-memory database
- Exposes a REST API endpoint to retrieve customer statistics
- Provides comprehensive logging using Serilog
- Includes Swagger/OpenAPI documentation for easy API exploration

## Architecture

### Core Components

#### 1. **WebSocket Consumer Service** (`WebSocketConsumerService`)
A background service that maintains a persistent WebSocket connection to receive real-time betting data.

**Key Features:**
- Automatic connection management with graceful error handling
- Processes three message types:
  - `Fixture`: Sports fixtures/games with available betting outcomes
  - `BetPlaced`: Customer bet placements with stake, odds, and fixture references
  - `EndOfFeed`: Signal to close the connection gracefully
- Resilient to connection interruptions
- Stores all received data in the in-memory database

**Implementation Details:**
- Uses `ClientWebSocket` for WebSocket communication
- Deserializes JSON messages using two-stage parsing (BaseMessage → specific payload type)
- Runs as an `IHostedService` background task
- Configurable WebSocket endpoint via `appsettings.json`

#### 2. **Customer Stats API** (`CustomerControllerEndpoints`)
REST API endpoint that aggregates customer betting data and retrieves customer information.

**Endpoint:**
```
GET /customer/{customerId}/stats
```

**Response:**
```json
{
  "customerId": 123,
  "name": "John Doe",
  "totalStandToWin": 1250.50
}
```

**Implementation Details:**
- Validates customer exists by checking placed bets
- Fetches customer name from external API
- Calculates total potential payout by summing all bets: `Σ(stake × odds)`
- Returns 404 if customer not found or has no bets
- Includes performance logging with request timing

#### 3. **In-Memory Database** (`MyDummyDatabase`)
Thread-safe, in-memory data storage using concurrent collections.

**Key Features:**
- Concurrent read/write operations using `ConcurrentDictionary`
- Stores fixtures by ID for quick lookup
- Stores all placed bets with duplicate detection
- Provides read-only collections for data access
- Structured logging for all database operations

**Data Structures:**
- `_fixtures`: Dictionary keyed by fixture ID
- `_placedBets`: Dictionary using bet payload as key

#### 4. **Configuration Singleton** (`ConfigurationSingleton`)
Centralized configuration management ensuring single configuration instance across the application.

**Features:**
- Lazy initialization pattern
- Environment-specific configuration support
- Supports `appsettings.json` and `appsettings.{Environment}.json`
- Environment variable overrides

## Data Models

### BaseMessage
Envelope for all WebSocket messages with message type discrimination.

```csharp
public record BaseMessage
{
    public MessageType Type { get; init; }
    public JsonElement Payload { get; init; }
    public DateTime Timestamp { get; init; }
}
```

### FixturePayload
Sports fixture/game with available betting outcomes.

```csharp
public record FixturePayload
{
    public long Id { get; init; }
    public DateTime StartTime { get; init; }
    public string Name { get; init; }
    public IReadOnlyList<Outcome> Outcomes { get; init; }
}
```

### BetPlacedPayload
Customer bet with automatic potential payout calculation.

```csharp
public record BetPlacedPayload
{
    public long FixtureId { get; init; }
    public string OutcomeKey { get; init; }
    public long CustomerId { get; init; }
    public decimal Odds { get; init; }
    public decimal Stake { get; init; }
    public decimal PotentialPayout => Stake * Odds;
}
```

## Technology Stack

- **.NET 10.0** - Latest .NET framework
- **ASP.NET Core** - Web framework and hosting
- **Serilog** (v9.0.0) - Structured logging
- **Swashbuckle/OpenAPI** (v9.0.3) - API documentation
- **System.Net.WebSockets** - WebSocket client
- **Entity Framework Core** (v9.0.7) - ORM (referenced but not actively used)
- **Concurrent Collections** - Thread-safe in-memory storage

## Configuration

### Required Settings (appsettings.json)

```json
{
  "WebSocketEndpoint": "wss://your-websocket-endpoint.com",
  "CustomerDetailsApiEndpoint": "https://your-api-endpoint.com",
  "CandidateId": "your-candidate-id",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Configuration Keys

| Key | Description | Required |
|-----|-------------|----------|
| `WebSocketEndpoint` | WebSocket server URL for receiving betting data | Yes |
| `CustomerDetailsApiEndpoint` | External API for fetching customer information | Yes |
| `CandidateId` | Candidate identifier for API authentication | Yes |

## Getting Started

### Prerequisites

- .NET 10.0 SDK or later
- Visual Studio 2023+, VS Code, or JetBrains Rider

### Installation

1. **Clone the repository:**
   ```bash
   git clone <repository-url>
   cd fdj-coding-challenge
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Configure application settings:**
   Update `FdjCodeChallenge.Api/appsettings.json` or `appsettings.Development.json` with your endpoints and candidate ID.

### Running the Application

#### Using .NET CLI:
```bash
cd FdjCodeChallenge.Api
dotnet run
```

#### Using Visual Studio:
1. Open `FdjCodeChallenge.sln`
2. Set `FdjCodeChallenge.Api` as startup project
3. Press F5 or click Run

#### Using VS Code:
1. Open the workspace folder
2. Use the provided build task: `Terminal` → `Run Task` → `build`
3. Start with F5 or use `dotnet run`

### Building the Project

```bash
dotnet build
```

Or use the VS Code task:
```bash
Terminal → Run Task → build
```

## API Documentation

Once running, access the Swagger UI at:
```
https://localhost:<port>/swagger
```

### Available Endpoints

#### Get Customer Statistics
- **Endpoint:** `GET /customer/{customerId}/stats`
- **Description:** Retrieves customer name and total potential payout
- **Parameters:**
  - `customerId` (path, long) - The customer's unique identifier
- **Responses:**
  - `200 OK` - Customer stats successfully retrieved
  - `404 Not Found` - Customer not found or has no bets

**Example Request:**
```bash
curl -X GET "https://localhost:5001/customer/123/stats"
```

**Example Response:**
```json
{
  "customerId": 123,
  "name": "John Doe",
  "totalStandToWin": 1250.50
}
```

## Project Structure

```
FdjCodeChallenge.Api/
├── Controllers/
│   └── CustomerControllerEndpoints.cs    # REST API endpoints
├── Database/
│   ├── FdjDatabase.cs                    # Database interface
│   └── MyDummyDatabase.cs                # In-memory implementation
├── Models/
│   ├── BaseMessage.cs                    # WebSocket message envelope
│   ├── BetPlacedPayload.cs              # Bet placement model
│   ├── EndOfFeedPayload.cs              # Feed termination signal
│   └── FixturePayload.cs                # Fixture/game model
├── Services/
│   └── WebSocketConsumerService.cs       # Background WebSocket consumer
├── Utilities/
│   └── ConfigurationSingleton.cs         # Configuration management
├── Program.cs                            # Application entry point
├── appsettings.json                      # Configuration
└── FdjCodeChallenge.Api.csproj          # Project file
```

## Design Patterns & Best Practices

### Design Patterns Used

1. **Singleton Pattern** - `ConfigurationSingleton` ensures single configuration instance
2. **Repository Pattern** - `MyDummyDatabase` abstracts data storage
3. **Background Service Pattern** - `WebSocketConsumerService` for long-running tasks
4. **Record Types** - Immutable data models using C# records
5. **Dependency Injection** - Constructor injection throughout the application

### Best Practices Implemented

- **Structured Logging:** Comprehensive logging with Serilog and structured properties
- **Centralized Configuration:** Single source of truth for app configuration
- **Thread Safety:** Concurrent collections for safe multi-threaded access
- **Error Handling:** Graceful error handling with proper exception catching
- **Minimal APIs:** Modern ASP.NET Core endpoint mapping
- **Nullable Reference Types:** Enabled for better null safety
- **Records for DTOs:** Immutable data transfer objects
- **Source Generated Logging:** Performance-optimized logging (see `MyDummyDatabase` partial class)
- **Central Package Management:** Version management via `Directory.Packages.props`

## Implementation Highlights

### WebSocket Message Processing
The application uses a two-stage deserialization approach:
1. First deserialize to `BaseMessage` to determine message type
2. Then deserialize the `Payload` property to the specific type

This allows type-safe handling of polymorphic messages from the WebSocket stream.

### Potential Payout Calculation
The `BetPlacedPayload` includes a computed property for automatic payout calculation:
```csharp
public decimal PotentialPayout => Stake * Odds;
```

Customer total is aggregated by summing all bet payouts:
```csharp
var totalStandToWin = database.PlacedBets
    .Where(bet => bet.CustomerId == customerId)
    .Sum(bet => bet.PotentialPayout);
```

### Concurrent Data Access
The in-memory database uses `ConcurrentDictionary` for thread-safe operations without explicit locking:
```csharp
private readonly ConcurrentDictionary<long, FixturePayload> _fixtures = new();
private readonly ConcurrentDictionary<BetPlacedPayload, BetPlacedPayload> _placedBets = new();
```

## Future Enhancements

Potential improvements for production deployment:

1. **Persistent Storage:** Replace in-memory database with PostgreSQL, SQL Server, or MongoDB
2. **Caching:** Add Redis for frequently accessed customer data
3. **Authentication:** Implement JWT or API key authentication
4. **Rate Limiting:** Add rate limiting to prevent API abuse
5. **Health Checks:** Add health check endpoints for monitoring
6. **Metrics:** Integrate Application Insights or Prometheus
7. **Circuit Breaker:** Add Polly for resilient HTTP calls
8. **Horizontal Scaling:** Design for multiple instances with shared state
9. **Message Queue:** Use RabbitMQ or Azure Service Bus for bet processing
10. **Unit Tests:** Add comprehensive test coverage
11. **Integration Tests:** Test WebSocket and API interactions
12. **Docker Support:** Add Dockerfile and docker-compose
13. **CI/CD Pipeline:** GitHub Actions or Azure DevOps pipelines

## Troubleshooting

### Common Issues

**WebSocket Connection Fails:**
- Verify `WebSocketEndpoint` in configuration
- Check network connectivity and firewall rules
- Ensure candidate ID is valid

**Customer API Returns 404:**
- Verify `CustomerDetailsApiEndpoint` is correct
- Ensure customer has placed at least one bet
- Check customer ID exists in external system

**Application Won't Start:**
- Verify .NET 10.0 SDK is installed: `dotnet --version`
- Check for port conflicts
- Review startup logs for configuration errors
