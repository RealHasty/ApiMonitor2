using System.Diagnostics;
using ApiMonitor.Models;

namespace ApiMonitor.Services;

/// <summary>
/// Tests HTTP connectivity to an API (base URL or a specific endpoint).
/// Uses the IHttpClient registered via builder.Services.AddHttpClient().
/// A Stopwatch wraps the GetAsync call so ResponseTimeMs is accurate.
/// </summary>
public class ApiConnectionService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ApiConnectionService> _logger;

    public ApiConnectionService(IHttpClientFactory httpClientFactory,
                                ILogger<ApiConnectionService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    // -------------------------------------------------------
    // Test the API's base URL
    // -------------------------------------------------------
    public async Task<ConnectionResult> TestAsync(string url)
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(10);

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await client.GetAsync(url);
            sw.Stop(); // <-- stops immediately after GetAsync returns

            return new ConnectionResult
            {
                StatusCode    = (int)response.StatusCode,
                ResponseTimeMs = sw.ElapsedMilliseconds,
                IsRunning     = response.IsSuccessStatusCode
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Connection failed for {Url}", url);

            return new ConnectionResult
            {
                StatusCode     = 0,
                ResponseTimeMs = sw.ElapsedMilliseconds,
                IsRunning      = false,
                ErrorMessage   = ex.Message
            };
        }
    }

    // -------------------------------------------------------
    // Test a specific endpoint (base URL + path)
    // -------------------------------------------------------
    public async Task<ConnectionResult> TestEndpointAsync(string baseUrl, string path)
    {
        var fullUrl = baseUrl.TrimEnd('/') + "/" + path.TrimStart('/');
        return await TestAsync(fullUrl);
    }

    // -------------------------------------------------------
    // Test all endpoints on an Api and update their status
    // -------------------------------------------------------
    public async Task TestAllEndpointsAsync(Api api)
    {
        // First test the base URL
        var baseResult = await TestAsync(api.Url);
        api.StatusCode     = baseResult.StatusCode;
        api.ResponseTimeMs = baseResult.ResponseTimeMs;
        api.IsRunning      = baseResult.IsRunning;

        // Then each endpoint
        foreach (var endpoint in api.Endpoints)
        {
            var result = await TestEndpointAsync(api.Url, endpoint.Path);
            endpoint.StatusCode     = result.StatusCode;
            endpoint.ResponseTimeMs = result.ResponseTimeMs;
            endpoint.IsRunning      = result.IsRunning;
        }
    }
}

// -------------------------------------------------------
// Simple result DTO — no EF involvement
// -------------------------------------------------------
public class ConnectionResult
{
    public int    StatusCode     { get; set; }
    public long   ResponseTimeMs { get; set; }
    public bool   IsRunning      { get; set; }
    public string? ErrorMessage  { get; set; }
}
