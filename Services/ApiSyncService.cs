using ApiMonitor.Data;
using ApiMonitor.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiMonitor.Services;

// Reads all OpenAPI yaml files, upserts them into the DB, then tests connections.

public class ApiSyncService
{
    private readonly AppDbContext _db;
    private readonly YamlLoaderService _yamlLoader;
    private readonly ApiConnectionService _connectionService;

    public ApiSyncService(AppDbContext db, YamlLoaderService yamlLoader, ApiConnectionService connectionService)
    {
        _db = db;
        _yamlLoader = yamlLoader;
        _connectionService = connectionService;
    }

    public async Task SyncAllAsync(string host)
    {
        var yamlApis = _yamlLoader.LoadAll();

        foreach (var (relativePath, definition) in yamlApis)
        {
            // Resolve the base URL (replaces {host} with the default value)
            var server = definition.Servers.FirstOrDefault();
            var baseUrl = server != null ? _yamlLoader.ResolveServerUrl(server, host) : string.Empty;

            // Find existing DB record for this yaml file, or create a new one
            var existing = await _db.Apis
                .Include(a => a.Endpoints)
                .FirstOrDefaultAsync(a => a.YamlSource == relativePath);

            if (existing == null)
            {
                existing = new Api { YamlSource = relativePath };
                _db.Apis.Add(existing);
            }

            // Update fields from the yaml
            existing.Name = definition.Info.Title;
            existing.Version = definition.Info.Version;
            existing.Url = baseUrl;

            // Clear old endpoints and reload from yaml
            existing.Endpoints.Clear();

            // Loop through each path and method in the OpenAPI spec
            // paths looks like: { "/health": { "get": { summary: ... } } }
            foreach (var path in definition.Paths)
            {
                var pathString = path.Key; // e.g. /fetchCardDetails

                foreach (var method in path.Value)
                {
                    var httpMethod = method.Key.ToUpper(); // e.g. POST
                    var operation = method.Value;

                    // Pull out the $ref for request schema if there is one
                    string? requestRef = null;
                    if (operation.RequestBody?.Content.ContainsKey("application/json") == true)
                    {
                        operation.RequestBody.Content["application/json"].Schema
                            .TryGetValue("$ref", out requestRef);
                    }

                    // Pull required header names from parameters
                    var headers = operation.Parameters
                        .Where(p => p.ContainsKey("$ref"))
                        .Select(p => p["$ref"].Split('/').Last()) // grab last part of $ref path
                        .ToList();

                    existing.Endpoints.Add(new ApiEndpoint
                    {
                        Method          = httpMethod,
                        Path            = pathString,
                        Summary         = operation.Summary,
                        Description     = operation.Description,
                        OperationId     = operation.OperationId,
                        RequestSchemaRef = requestRef,
                        RequiredHeaders = headers.Any() ? string.Join(",", headers) : null
                    });
                }
            }

            await _db.SaveChangesAsync();
        }
    }
}
