namespace NewsAggregator.Web.Services;

// Primary Constructor (C# 12+): Injects IHttpClientFactory and ILogger directly into the class.
// BackgroundService is a built-in .NET base class for running long-lived or startup background tasks.
public class N8nStartupTriggerService(
    IHttpClientFactory httpClientFactory,
    ILogger<N8nStartupTriggerService> logger) : BackgroundService
{
    // ExecuteAsync runs automatically when the ASP.NET Core application starts up.
    // 'stoppingToken' is triggered if the app shuts down, allowing graceful cancellation.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait 3 seconds so Kestrel web server (port 5223) finishes booting up
        // before n8n immediately calls back into /api/articles/ endpoint.
        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

        try
        {
            // Use IHttpClientFactory to create a managed HttpClient (prevents socket exhaustion).
            var client = httpClientFactory.CreateClient();

            // Send an empty POST request to n8n's production webhook URL.
            // 'await' pauses this method without blocking the main web server thread.
            var response = await client.PostAsync(
                "http://localhost:5678/webhook/sync-news",
                content: null,
                stoppingToken);

            // Structured logging: {StatusCode} is logged as a named property.
            logger.LogInformation("n8n startup sync triggered. Status: {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            // Catch network errors (e.g., if Docker/n8n isn't running yet) so the web app doesn't crash.
            logger.LogWarning("Could not reach n8n on startup: {Message}", ex.Message);
        }
    }
}
