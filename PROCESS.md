API Monitor — Build Checklist
Folder Structure

 Create APIData/ folder in project root
 Create APIData/schemas/common/
 Create APIData/schemas/get/response/
 Create APIData/schemas/post/request/ and post/response/
 Create APIData/schemas/patch/request/ and patch/response/
 Create Data/ folder
 Create Services/ folder


YAML Files

 Drop the real yaml file into APIData/
 Verify the structure has openapi, info, servers, and paths sections
 Verify schemas/common/ has the headers file with ClientID and ClientName


Models

 Create Models/ConnectionResult.cs — 4 properties: StatusCode, ResponseTimeMs, IsRunning, ErrorMessage
 Create Models/YamlApiDefinition.cs — classes that mirror the OpenAPI yaml structure:

OpenApiDefinition (Info, Servers, Paths)
OpenApiInfo (Title, Version, Description)
OpenApiServer (Url, Variables)
OpenApiServerVariable (Default, Description)
OpenApiOperation (Summary, Description, OperationId, Parameters, RequestBody, Responses)
OpenApiRequestBody (Required, Content)
OpenApiMediaType (Schema)
OpenApiResponse (Description)


 Create Models/Api.cs — two classes in one file:

Api — (Id, Name, Url, Version, YamlSource, StatusCode, ResponseTimeMs, IsRunning, List of Endpoints)
ApiEndpoint — (Id, ApiId, Method, Path, Summary, Description, OperationId, RequestSchemaRef, RequiredHeaders, StatusCode, ResponseTimeMs, IsRunning, Api navigation property)




Database

 Create Data/AppDbContext.cs — inherit DbContext, constructor takes DbContextOptions<AppDbContext>, two DbSet properties (Apis and ApiEndpoints), no OnModelCreating needed
 Update appsettings.json:

json"ConnectionStrings": {
  "DefaultConnection": "Data:DATABASE;Initial Catalog=ApiMonitorDb;Integrated Security=True;MultipleActiveResultSets=True;TrustServerCertificate=True;"
}

 Run Add-Migration InitialCreate in PMC
 Check the migration Up() is not empty — fill it in manually if it is
 Run Update-Database
 Verify in SQL that Apis and ApiEndpoints tables exist


Services

 Create Services/YamlLoaderService.cs:

Constructor takes IWebHostEnvironment
LoadAll() — scans APIData/ top level only for .yaml files, deserializes each into OpenApiDefinition, returns list of (relativePath, definition) tuples
ResolveServerUrl(OpenApiServer server, string host) — replaces {host} in the URL with the host value entered by the user


 Create Services/ApiConnectionService.cs:

Constructor takes IHttpClientFactory
TestAsync(string url) — starts stopwatch, calls GetAsync, stops stopwatch, returns ConnectionResult
TestAllEndpointsAsync(Api api) — tests base URL then each endpoint, updates their StatusCode, ResponseTimeMs, IsRunning


 Create Services/ApiSyncService.cs:

Constructor takes AppDbContext, YamlLoaderService, ApiConnectionService
SyncAllAsync(string host) — calls LoadAll(), loops through results, finds or creates DB record by YamlSource, updates fields, clears and reloads endpoints, saves. Does NOT test connections here.




Program.cs

 Register services in this order:

csharpbuilder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHttpClient();
builder.Services.AddScoped<YamlLoaderService>();
builder.Services.AddScoped<ApiConnectionService>();
builder.Services.AddScoped<ApiSyncService>();

 Add startup block that only migrates — no sync on startup:

csharpusing (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

Controllers

 Create HomeController.cs — just Index() with total/running/down counts
 Create ApiController.cs with these actions in order:

 Index() — list all APIs from DB with endpoints
 Details(int id) — single API with endpoints, tests connection on open
 Call() GET — loads endpoint, returns form view
 Call() POST — builds URL, adds headers, sends HTTP request, shows response
 Create() GET/POST — manual add with connection test on save
 Edit() GET/POST — edit with connection test on save
 Delete() GET/POST — confirm and delete
 Test(int id) POST — AJAX live test, returns JSON
 Sync() GET — returns host entry form
 Sync(string host) POST — calls SyncAllAsync(host) then tests all connections and saves




Views

 Create Views/_ViewImports.cshtml — @using and @addTagHelper
 Create Views/_ViewStart.cshtml — sets Layout = "_Layout"
 Create Views/Shared/_Layout.cshtml — navbar, container, alert display, scripts
 Create Views/Home/Index.cshtml — dashboard with 3 stat cards (Total, Online, Offline) and Sync link
 Create Views/Api/Index.cshtml — grid of API cards with status badges and action buttons
 Create Views/Api/Details.cshtml — endpoint list with method badges, status, Call buttons
 Create Views/Api/Call.cshtml — form with ClientID/ClientName headers, input fields per endpoint, response box
 Create Views/Api/Sync.cshtml — form with one input for host IP, submit triggers sync
 Create Views/Api/Create.cshtml — simple form for Name and URL
 Create Views/Api/Edit.cshtml — same as Create but pre-filled
 Create Views/Api/Delete.cshtml — confirm delete page


Static Files

 Create wwwroot/css/site.css — dark theme styles
 Create wwwroot/js/site.js — testApi() function that calls /Api/Test/{id} via fetch and updates status badge without page reload


Final Verification

 App starts without errors
 Click ↺ Sync YAML, enter the host IP when prompted
 Verify APIs and endpoints show up in the UI
 Verify /health endpoint shows correct status after sync
 Click Call on /health, fill in ClientID and ClientName, confirm response comes back
 Once /health works, move to the next endpoint


Key Things to Remember

Models first — everything else depends on them
Services in order: Loader → Connection → Sync
DbSet in AppDbContext is how EF knows which tables exist
ApiId on ApiEndpoint is the foreign key — EF links it to Api.Id by naming convention automatically
YamlApiDefinition is temporary (just for reading the file), Api is permanent (saved to DB)
{host} gets replaced in ResolveServerUrl() with whatever the user types into the Sync form
If migration Up() is empty — fill it manually, delete the row from __EFMigrationsHistory, then re-run Update-Database