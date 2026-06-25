using ApiMonitor.Data;
using ApiMonitor.Models;
using ApiMonitor.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace ApiMonitor.Controllers;

public class ApiController : Controller
{
    private readonly AppDbContext _db;
    private readonly ApiSyncService _syncService;
    private readonly ApiConnectionService _connectionService;
    private readonly IHttpClientFactory _httpClientFactory;

    public ApiController(AppDbContext db, ApiSyncService syncService,
                         ApiConnectionService connectionService,
                         IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _syncService = syncService;
        _connectionService = connectionService;
        _httpClientFactory = httpClientFactory;
    }

    // List all APIs
    public async Task<IActionResult> Index()
    {
        var apis = await _db.Apis.Include(a => a.Endpoints).OrderBy(a => a.Name).ToListAsync();
        return View(apis);
    }

    // Details page - shows all endpoints, retests connection on open
    public async Task<IActionResult> Details(int id)
    {
        var api = await _db.Apis.Include(a => a.Endpoints).FirstOrDefaultAsync(a => a.Id == id);
        if (api == null) return NotFound();

        await _connectionService.TestAllEndpointsAsync(api);
        await _db.SaveChangesAsync();

        return View(api);
    }

    // -------------------------------------------------------
    // CALL ENDPOINT — the page where you enter a card number
    // and send a real request to the API
    // -------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Call(int endpointId)
    {
        var endpoint = await _db.ApiEndpoints
            .Include(e => e.Api)
            .FirstOrDefaultAsync(e => e.Id == endpointId);

        if (endpoint == null) return NotFound();

        return View(endpoint);
    }

    [HttpPost]
    public async Task<IActionResult> Call(int endpointId, string clientId, string clientName, string requestBody)
    {
        var endpoint = await _db.ApiEndpoints
            .Include(e => e.Api)
            .FirstOrDefaultAsync(e => e.Id == endpointId);

        if (endpoint == null) return NotFound();

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(5);

        // Build the full URL: base URL + path
        var fullUrl = endpoint.Api.Url.TrimEnd('/') + endpoint.Path;

        // Add required headers
        client.DefaultRequestHeaders.Add("ClientID", clientId);
        client.DefaultRequestHeaders.Add("ClientName", clientName);

        HttpResponseMessage response;

        try
        {
            if (endpoint.Method == "GET")
            {
                response = await client.GetAsync(fullUrl);
            }
            else if (endpoint.Method == "POST")
            {
                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                response = await client.PostAsync(fullUrl, content);
            }
            else if (endpoint.Method == "PATCH")
            {
                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                response = await client.PatchAsync(fullUrl, content);
            }
            else
            {
                ViewBag.Error = $"Method {endpoint.Method} not supported yet.";
                return View(endpoint);
            }

            // Pretty print the JSON response
            var raw = await response.Content.ReadAsStringAsync();
            string pretty;
            try
            {
                var parsed = JsonDocument.Parse(raw);
                pretty = JsonSerializer.Serialize(parsed, new JsonSerializerOptions { WriteIndented = true });
            }
            catch
            {
                pretty = raw; // not JSON, just show as-is
            }

            ViewBag.StatusCode = (int)response.StatusCode;
            ViewBag.Response   = pretty;
            ViewBag.IsSuccess  = response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            ViewBag.Error = ex.Message;
        }

        return View(endpoint);
    }

    // Create manually
    [HttpGet]
    public IActionResult Create() => View(new Api());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Api api)
    {
        if (!ModelState.IsValid) return View(api);

        var result = await _connectionService.TestAsync(api.Url);
        api.StatusCode     = result.StatusCode;
        api.ResponseTimeMs = result.ResponseTimeMs;
        api.IsRunning      = result.IsRunning;

        _db.Apis.Add(api);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Added \"{api.Name}\"";
        return RedirectToAction(nameof(Index));
    }

    // Edit
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var api = await _db.Apis.Include(a => a.Endpoints).FirstOrDefaultAsync(a => a.Id == id);
        if (api == null) return NotFound();
        return View(api);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Api api)
    {
        if (id != api.Id) return BadRequest();
        if (!ModelState.IsValid) return View(api);

        var result = await _connectionService.TestAsync(api.Url);
        api.StatusCode     = result.StatusCode;
        api.ResponseTimeMs = result.ResponseTimeMs;
        api.IsRunning      = result.IsRunning;

        _db.Update(api);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Updated \"{api.Name}\"";
        return RedirectToAction(nameof(Index));
    }

    // Delete
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var api = await _db.Apis.FindAsync(id);
        if (api == null) return NotFound();
        return View(api);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var api = await _db.Apis.FindAsync(id);
        if (api != null) { _db.Apis.Remove(api); await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }

    // Live test button (AJAX)
    [HttpPost]
    public async Task<IActionResult> Test(int id)
    {
        var api = await _db.Apis.Include(a => a.Endpoints).FirstOrDefaultAsync(a => a.Id == id);
        if (api == null) return NotFound();

        await _connectionService.TestAllEndpointsAsync(api);
        await _db.SaveChangesAsync();

        return Json(new
        {
            api.IsRunning,
            api.StatusCode,
            api.ResponseTimeMs,
            endpoints = api.Endpoints.Select(ep => new
            {
                ep.Id, ep.Method, ep.Path, ep.IsRunning, ep.StatusCode, ep.ResponseTimeMs
            })
        });
    }

    // Sync all yaml files
    [HttpPost]
    public async Task<IActionResult> Sync()
    {
        await _syncService.SyncAllAsync();

        // Test all connections after manual sync
        var apis = await _db.Apis.Include(a => a.Endpoints).ToListAsync();
        foreach (var api in apis)
        {
            await _connectionService.TestAllEndpointsAsync(api);
        }
        await _db.SaveChangesAsync();

        TempData["Success"] = "YAML sync complete — all APIs tested.";
        return RedirectToAction(nameof(Index));
    }
}
