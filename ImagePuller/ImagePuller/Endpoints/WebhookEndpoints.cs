using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace ImagePuller.Endpoints;

public static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/webhook", async (HttpContext context, IConfiguration config, ILogger<Program> logger) =>
        {
            var secret = config["Webhook:Secret"];
            var scriptPath = config["Webhook:DeploymentScriptPath"];

            if (string.IsNullOrEmpty(secret))
            {
                logger.LogError("Webhook secret is not configured.");
                return Results.StatusCode(500);
            }

            if (!context.Request.Headers.TryGetValue("X-Hub-Signature-256", out var signature))
            {
                logger.LogWarning("Missing X-Hub-Signature-256 header.");
                return Results.Unauthorized();
            }

            // Read the body for signature verification
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            if (!VerifySignature(body, signature, secret))
            {
                logger.LogWarning("Invalid webhook signature.");
                return Results.Unauthorized();
            }

            logger.LogInformation("Webhook verified. Triggering deployment script: {ScriptPath}", scriptPath);

            // Run deployment in the background
            _ = Task.Run(async () =>
            {
                try
                {
                    await ExecuteScript(scriptPath, logger);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to execute deployment script.");
                }
            });

            return Results.Accepted();
        });
    }

    private static bool VerifySignature(string body, string? signature, string secret)
    {
        if (string.IsNullOrEmpty(signature) || !signature.StartsWith("sha256=")) return false;

        var sha256 = signature.Substring(7);
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var bodyBytes = Encoding.UTF8.GetBytes(body);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(bodyBytes);
        var hash = Convert.ToHexString(hashBytes).ToLower();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(hash), 
            Encoding.UTF8.GetBytes(sha256));
    }

    private static async Task ExecuteScript(string? scriptPath, ILogger logger)
    {
        if (string.IsNullOrEmpty(scriptPath) || !File.Exists(scriptPath))
        {
            logger.LogError("Deployment script not found at {ScriptPath}", scriptPath);
            return;
        }

        logger.LogInformation("Executing script: {ScriptPath}", scriptPath);

        var processInfo = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = scriptPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processInfo };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            logger.LogError("Script failed with exit code {ExitCode}. Error: {Error}", process.ExitCode, error);
        }
        else
        {
            logger.LogInformation("Script executed successfully. Output: {Output}", output);
        }
    }
}
