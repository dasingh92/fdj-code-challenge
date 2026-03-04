using FdjCodeChallenge.Api.Utilities;
using Serilog;
using FdjCodeChallenge.Api.Services;
using FdjCodeChallenge.Api.Database;
using FdjCodeChallenge.Api.Controllers;

using var log = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
try
{
    log.Information("Starting web host...");
    var builder = WebApplication.CreateBuilder(args);
    // Use our configuration singleton to ensure that we are using the same configuration throughout the application and that we are not creating multiple instances of the configuration.
    var configuration = ConfigurationSingleton.Instance;

    // Configure Serilog to use our configuration and to write logs to the console. In a real application, we might want to write logs to a file or to a logging service, but for the sake of this challenge we can write logs to the console.
    builder.Logging.ClearProviders();

    builder.Host.UseSerilog((context, configuration) =>
    {
        configuration.ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console();
    });
    builder.Services.AddLogging();
    builder.Services.AddControllers();
    // builder.Services.AddDbContext<DbContext>(options =>
    //     options.UseInMemoryDatabase("InMemoryDb"));
    builder.Services.AddSingleton<MyDummyDatabase>();
    builder.Services.AddHostedService<WebSocketConsumerService>();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddHttpClient();
    var app = builder.Build();
    if( app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    app.UseRouting();
    app.UseHttpsRedirection();
    app.MapControllers();
    app.MapCustomerControllerEndpoints();

    app.Run();
}
catch(Exception ex)
{
    log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
