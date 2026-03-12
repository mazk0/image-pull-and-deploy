using ImagePuller.Endpoints;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();

app.UseSerilogRequestLogging();

// Register Endpoints
app.MapWebhookEndpoints();

app.Run();
