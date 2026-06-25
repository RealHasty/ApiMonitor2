Step 5 — Create the Models
Do models before anything else because everything else depends on them.
Models/Api.cs — two classes in one file:

Api — the database table (Id, Name, Url, Version, YamlSource, StatusCode, ResponseTimeMs, IsRunning, List of Endpoints)
ApiEndpoint — the endpoints table (Id, ApiId, Method, Path, Summary, Description, OperationId, RequestSchemaRef, RequiredHeaders, StatusCode, ResponseTimeMs, IsRunning, and the Api navigation property)

Models/YamlApiDefinition.cs — classes that mirror the OpenAPI YAML structure for deserialization:

OpenApiDefinition (Info, Servers, Paths)
OpenApiInfo (Title, Version, Description)
OpenApiServer (Url, Variables)
OpenApiServerVariable (Default, Description)
OpenApiOperation (Summary, Description, OperationId, Parameters, RequestBody, Responses)
OpenApiRequestBody (Required, Content)
OpenApiMediaType (Schema)
OpenApiResponse (Description)

Models/ConnectionResult.cs — simple class to hold test results (StatusCode, ResponseTimeMs, IsRunning, ErrorMessage)

Step 6 — Create the Database Context
Data/AppDbContext.cs

Inherit from DbContext
Constructor takes DbContextOptions<AppDbContext>
Two DbSet properties: Apis and ApiEndpoints
No OnModelCreating needed — EF figures it out from the models


Step 7 — Update appsettings.json
Add your connection string:
json"ConnectionStrings": {
  "DefaultConnection": "Data Source=DESKTOP-NCP3T8N\\SQLEXPRESS;Initial Catalog=ApiMonitorDb;Integrated Security=True;MultipleActiveResultSets=True;TrustServerCertificate=True;"
}

Step 8 — Create the Services
Build them in this order because each one depends on the previous:
Services/YamlLoaderService.cs first — it only depends on IWebHostEnvironment:

Constructor takes IWebHostEnvironment
LoadAll() — scans APIData/ top level only for .yaml files, deserializes each into OpenApiDefinition, returns list of (relativePath, definition) tuples
ResolveServerUrl() — replaces {host} in the URL with the default value from the variables

Services/ApiConnectionService.cs second — it only depends on IHttpClientFactory:

Constructor takes IHttpClientFactory
TestAsync(string url) — starts stopwatch, calls GetAsync, stops stopwatch, returns ConnectionResult
TestAllEndpointsAsync(Api api) — tests base URL then each endpoint, updates their StatusCode/ResponseTimeMs/IsRunning

Services/ApiSyncService.cs third — it depends on the other two services:

Constructor takes AppDbContext, YamlLoaderService, ApiConnectionService
SyncAllAsync() — calls LoadAll(), loops through results, finds or creates DB record by YamlSource, updates fields, clears and reloads endpoints, saves. Does NOT test connections here.


Step 9 — Update Program.cs
Register everything in this order:
csharpbuilder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHttpClient();
builder.Services.AddScoped<YamlLoaderService>();
builder.Services.AddScoped<ApiConnectionService>();
builder.Services.AddScoped<ApiSyncService>();
Then in the startup block after var app = builder.Build():
csharpusing (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    var sync = scope.ServiceProvider.GetRequiredService<ApiSyncService>();
    await sync.SyncAllAsync();
}

Step 10 — Run Migrations
In Package Manager Console:
powershellAdd-Migration InitialCreate
Update-Database
If the migration Up() is empty, fill it in manually with CreateTable for Apis and ApiEndpoints. Then delete the entry from __EFMigrationsHistory in SQL and re-run Update-Database.

Step 11 — Create the Controller
Controllers/ApiController.cs — actions in this order:

Index() — gets all APIs from DB with endpoints, returns view
Details(int id) — gets one API, tests connection, returns view
Call(int endpointId) GET — loads endpoint, returns form view
Call(int endpointId, ...) POST — builds URL, adds headers, sends HTTP request, shows response
Create() GET/POST — manual add with connection test on save
Edit() GET/POST — edit with connection test on save
Delete() GET/POST — confirm and delete
Test(int id) POST — AJAX live test, returns JSON
Sync() POST — calls SyncAllAsync() then tests all connections

Controllers/HomeController.cs — just Index() that counts total/running/down and passes to view

Step 12 — Create the Views
Build them in this order:
Shared first:

Views/_ViewImports.cshtml — @using and @addTagHelper
Views/_ViewStart.cshtml — sets Layout = "_Layout"
Views/Shared/_Layout.cshtml — navbar, container, alert display, scripts

Then each view:

Views/Home/Index.cshtml — dashboard with 3 stat cards (Total, Online, Offline) and Sync button
Views/Api/Index.cshtml — grid of API cards with status badge, meta info, action buttons
Views/Api/Details.cshtml — endpoint list with method badges, status, Call button per endpoint
Views/Api/Call.cshtml — form with ClientID/ClientName headers, input fields based on endpoint, response box
Views/Api/Create.cshtml — simple form for Name and URL
Views/Api/Edit.cshtml — same as Create but pre-filled
Views/Api/Delete.cshtml — confirm delete page


Step 13 — Add Static Files
wwwroot/css/site.css — all the dark theme styles

wwwroot/js/site.js — the testApi() function that calls /Api/Test/{id} via fetch and updates the badge without reloading

Step 14 — Final Checks

Drop a real .yaml file in APIData/ matching the OpenAPI structure
Update the host default value to the real server IP
Run the app and confirm the sync picks it up
Click ↺ Sync YAML to test connections manually
Click Call on an endpoint, fill in ClientID and the input, confirm the response comes back


Key Things to Remember

Models first, always — everything else depends on them
Services go in order: Loader → Connection → Sync
DbSet in AppDbContext is how EF knows which tables exist
ApiId on ApiEndpoint is the foreign key — EF links it to Api.Id automatically by naming convention
YamlApiDefinition is temporary (just for reading the file), Api is permanent (saved to DB)
{host} in the YAML URL gets replaced by ResolveServerUrl() before anything is saved
Migrations: if Up() is empty, fill it manually and clear __EFMigrationsHistory before re-running
