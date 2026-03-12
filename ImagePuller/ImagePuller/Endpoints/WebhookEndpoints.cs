using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace ImagePuller.Endpoints;

// Records for GitHub Webhook Payload
// Made properties optional (?) so 'ping' event doesn't fail validation
public record GitHubPackageVersion(
    [property: JsonPropertyName("tag")] string? Tag
);

public record GitHubPackage(
    [property: JsonPropertyName("package_version")] GitHubPackageVersion? PackageVersion
);

public record GitHubWebhookPayload(
    [property: JsonPropertyName("action")] string? Action,
    [property: JsonPropertyName("package")] GitHubPackage? Package
);

public static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/webhook", async (
            HttpContext context, 
            IConfiguration config, 
            ILogger<Program> logger,
            GitHubWebhookPayload payload) =>
        {
            var secret = config["Webhook:Secret"];
            var scriptPath = config["Webhook:DeploymentScriptPath"];

            if (string.IsNullOrEmpty(secret))
            {
                logger.LogError("Webhook secret is not configured.");
                return Results.StatusCode(500);
            }

            if (!context.Request.Headers.TryGetValue("X-Hub-Signature-256", out var signatureHeader))
            {
                logger.LogWarning("Missing X-Hub-Signature-256 header.");
                return Results.Unauthorized();
            }

            context.Request.Headers.TryGetValue("X-GitHub-Event", out var eventType);

            // IMPORTANT: We must rewind the body stream because the model binder (record) 
            // has already read it. EnableBuffering() must be called in Program.cs.
            context.Request.Body.Position = 0;
            using var ms = new MemoryStream();
            await context.Request.Body.CopyToAsync(ms);
            var bodyBytes = ms.ToArray();

            if (!VerifySignature(bodyBytes, signatureHeader.ToString(), secret))
            {
                logger.LogWarning("Invalid webhook signature.");
                return Results.Unauthorized();
            }

            if (eventType == "ping")
            {
                logger.LogInformation("GitHub ping received. Webhook is active.");
                return Results.Ok(new { message = "Ping received" });
            }

            // Extract logic from our validated records
            string action = payload.Action ?? "unknown";
            string tag = payload.Package?.PackageVersion?.Tag ?? "latest";

            logger.LogInformation("Webhook verified. Event: {Event}, Action: {Action}, Tag: {Tag}", eventType, action, tag);

            _ = Task.Run(async () =>
            {
                try { await ExecuteScript(scriptPath, $"{eventType} {action} {tag}", logger); }
                catch (Exception ex) { logger.LogError(ex, "Failed to execute deployment script."); }
            });

            return Results.Accepted();
        });
    }

    private static bool VerifySignature(byte[] bodyBytes, string signatureHeader, string secret)
    {
        if (!signatureHeader.StartsWith("sha256=")) return false;
        var headerHashHex = signatureHeader.Substring(7);
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        using var hmac = new HMACSHA256(keyBytes);
        var computedHashHex = Convert.ToHexString(hmac.ComputeHash(bodyBytes)).ToLower();
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(computedHashHex), Encoding.UTF8.GetBytes(headerHashHex));
    }

    private static async Task ExecuteScript(string? scriptPath, string arguments, ILogger logger)
    {
        if (string.IsNullOrEmpty(scriptPath) || !File.Exists(scriptPath))
        {
            logger.LogError("Deployment script not found at {ScriptPath}", scriptPath);
            return;
        }

        logger.LogInformation("Executing script: {ScriptPath} {Arguments}", scriptPath, arguments);
        var processInfo = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = $"{scriptPath} {arguments}",
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
        if (process.ExitCode != 0) logger.LogError("Script failed with exit code {ExitCode}. Error: {Error}", process.ExitCode, error);
        else logger.LogInformation("Script executed successfully. Output: {Output}", output);
    }
}
