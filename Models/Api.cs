namespace ApiMonitor.Models;

// -------------------------------------------------------
// Api = one row in the Apis table
// Represents one OpenAPI yaml file loaded from APIData/
// -------------------------------------------------------
public class Api
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;       // from info.title
    public string Url { get; set; } = string.Empty;        // resolved server URL
    public string Version { get; set; } = string.Empty;    // from info.version
    public string YamlSource { get; set; } = string.Empty; // which file this came from
    public int? StatusCode { get; set; }
    public long? ResponseTimeMs { get; set; }
    public bool IsRunning { get; set; }

    public List<ApiEndpoint> Endpoints { get; set; } = new();
}

// -------------------------------------------------------
// ApiEndpoint = one row in the ApiEndpoints table
// Represents one path+method combo from the OpenAPI spec
// e.g. POST /fetchCardDetails
// -------------------------------------------------------
public class ApiEndpoint
{
    public int Id { get; set; }
    public int ApiId { get; set; }                          // links back to Apis table

    public string Method { get; set; } = string.Empty;     // GET, POST, PATCH
    public string Path { get; set; } = string.Empty;       // /fetchCardDetails
    public string Summary { get; set; } = string.Empty;    // human readable name
    public string Description { get; set; } = string.Empty;
    public string OperationId { get; set; } = string.Empty;

    // The $ref paths pulled from the yaml for request/response schemas
    public string? RequestSchemaRef { get; set; }
    public string? ResponseSchemaRef { get; set; }

    // Headers required (comma separated e.g. "ClientID,ClientName")
    public string? RequiredHeaders { get; set; }

    // Last test result
    public int? StatusCode { get; set; }
    public long? ResponseTimeMs { get; set; }
    public bool IsRunning { get; set; }

    public Api Api { get; set; } = null!;
}
