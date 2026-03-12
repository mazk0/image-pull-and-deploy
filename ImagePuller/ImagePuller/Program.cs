using ImagePuller.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Using standard .NET Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

app.UseHttpsRedirection();

// Register Webhook Endpoints
app.MapWebhookEndpoints();

app.Run();
