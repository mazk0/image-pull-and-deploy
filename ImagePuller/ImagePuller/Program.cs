using ImagePuller.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Register the new .NET 10 validation services
builder.Services.AddValidation();

// Standard Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

// Enable buffering so we can read the body twice 
// (once for record validation, once for HMAC signature)
app.Use((context, next) =>
{
    context.Request.EnableBuffering();
    return next();
});

app.UseHttpsRedirection();

// Register Webhook Endpoints
app.MapWebhookEndpoints();

app.Run();
