using ApiMonitor.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ApiMonitor.Services;

// Reads every .yaml file in APIData/ and parses it as an OpenAPI 3.0 spec.
// Returns the parsed data so ApiSyncService can save it to the database.

public class YamlLoaderService
{
    private readonly IWebHostEnvironment _env;

    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public YamlLoaderService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public List<(string RelativePath, OpenApiDefinition Definition)> LoadAll()
    {
        var apiDataPath = Path.Combine(_env.ContentRootPath, "APIData");
        var results = new List<(string, OpenApiDefinition)>();

        if (!Directory.Exists(apiDataPath))
            return results;

        // Only load the top-level yaml files (the main spec files)
        // The schema files in subfolders are $ref'd, not loaded directly
        var files = Directory.GetFiles(apiDataPath, "*.yaml", SearchOption.TopDirectoryOnly)
            .Concat(Directory.GetFiles(apiDataPath, "*.yml", SearchOption.TopDirectoryOnly));

        foreach (var file in files)
        {
            try
            {
                var yaml = File.ReadAllText(file);
                var definition = _deserializer.Deserialize<OpenApiDefinition>(yaml);
                var relativePath = Path.GetRelativePath(apiDataPath, file);
                results.Add((relativePath, definition));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse {file}: {ex.Message}");
            }
        }

        return results;
    }

    // Resolves the server URL by replacing {host} with the default value
    // e.g. http://{host}:8080/api/v1 becomes http://192.168.1.100:8080/api/v1
    public string ResolveServerUrl(OpenApiServer server, string host)
    {
        var url = server.Url;
        url = url.Replace("{host}", host);
        return url;
    }
}
