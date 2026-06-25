namespace ApiMonitor.Models;

// -------------------------------------------------------
// These classes mirror the OpenAPI 3.0 yaml structure.
// YamlDotNet deserializes the file into OpenApiDefinition.
// -------------------------------------------------------

public class OpenApiDefinition
{
    public OpenApiInfo Info { get; set; } = new();
    public List<OpenApiServer> Servers { get; set; } = new();
    public Dictionary<string, Dictionary<string, OpenApiOperation>> Paths { get; set; } = new();
}

public class OpenApiInfo
{
    public string Title { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class OpenApiServer
{
    public string Url { get; set; } = string.Empty;
    public Dictionary<string, OpenApiServerVariable> Variables { get; set; } = new();
}

public class OpenApiServerVariable
{
    public string Default { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class OpenApiOperation
{
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OperationId { get; set; } = string.Empty;
    public List<Dictionary<string, string>> Parameters { get; set; } = new();
    public OpenApiRequestBody? RequestBody { get; set; }
    public Dictionary<string, OpenApiResponse> Responses { get; set; } = new();
}

public class OpenApiRequestBody
{
    public bool Required { get; set; }
    public Dictionary<string, OpenApiMediaType> Content { get; set; } = new();
}

public class OpenApiMediaType
{
    public Dictionary<string, string> Schema { get; set; } = new();
}

public class OpenApiResponse
{
    public string Description { get; set; } = string.Empty;
}
