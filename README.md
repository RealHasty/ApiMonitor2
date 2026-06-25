# API Monitor — MVC Web App

A .NET 10 MVC application that monitors REST APIs defined in YAML files.
Connects to SQL Server via EF Core, tests connectivity with a Stopwatch,
and syncs from the `APIData/` folder automatically on startup.

---

## Project Structure

```
ApiMonitor/
├── APIData/                  ← Drop your .yaml API definitions here
│   ├── sample-api.yaml
│   └── Schemas/
│       ├── GET/
│       ├── POST/
│       ├── PUT/
│       └── DELETE/
│
├── Controllers/
│   ├── HomeController.cs     ← Dashboard (total / online / offline counts)
│   └── ApiController.cs      ← Full CRUD + /Test + /Sync actions
│
├── Data/
│   └── AppDbContext.cs       ← EF Core DbContext (Apis + ApiEndpoints tables)
│
├── Migrations/               ← EF migration (auto-applied on startup)
│
├── Models/
│   ├── Api.cs                ← EF entities: Api, ApiEndpoint
│   └── YamlApiDefinition.cs  ← YAML deserialization POCOs
│
├── Services/
│   ├── YamlLoaderService.cs  ← Reads all .yaml files from APIData/ recursively
│   ├── ApiConnectionService.cs ← Tests URLs with HttpClient + Stopwatch
│   └── ApiSyncService.cs     ← Upserts YAML → DB, then tests connections
│
├── Views/
│   ├── Api/                  ← Index, Details, Create, Edit, Delete
│   └── Home/                 ← Dashboard
│
└── wwwroot/
    ├── css/site.css          ← Dark monitoring dashboard styles
    └── js/site.js            ← Live Test button (fetch → /Api/Test)
```

---

## Setup

### 1. Connection String

Edit `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=ApiMonitorDb;Trusted_Connection=True;"
}
```

For SQL Server Express:
```
Server=.\\SQLEXPRESS;Database=ApiMonitorDb;Trusted_Connection=True;TrustServerCertificate=True;
```

### 2. Run (DB migrates + YAML syncs automatically on first boot)

```bash
dotnet run
```

The app will:
1. Apply EF migrations (create tables if needed)
2. Scan `APIData/` for all `.yaml` / `.yml` files
3. Upsert every API + endpoints into the database
4. Test every connection with `HttpClient` + `Stopwatch`

### 3. Add Your YAML Files

Drop any `.yaml` file in `APIData/` using this structure:

```yaml
name: My API
url: https://api.example.com

endpoints:
  - method: GET
    path: /users
    description: Get all users
    schemaFile: Schemas/GET/users.json

  - method: POST
    path: /users
    description: Create a user
    schemaFile: Schemas/POST/create-user.json
```

Click **↺ Sync YAML** in the UI (or restart the app) to load new files.

---

## How the Connection Test Works

```
ApiConnectionService.TestAsync(url)
    │
    ├── var sw = Stopwatch.StartNew();
    ├── var response = await _httpClient.GetAsync(url);
    ├── sw.Stop();   ← stops immediately after GetAsync returns
    │
    └── returns ConnectionResult { StatusCode, ResponseTimeMs, IsRunning }
```

Connection is tested automatically:
- **On startup** — `ApiSyncService.SyncAllAsync()` called from `Program.cs`
- **On Details page open** — re-tested every visit
- **On Create** — tested before first DB insert
- **On Edit save** — re-tested every save
- **On "Test" button click** — AJAX call to `POST /Api/Test/{id}`, updates badge live

---

## Packages Used

| Package | Purpose |
|---|---|
| `Microsoft.EntityFrameworkCore.SqlServer` | ORM + SQL Server driver |
| `Microsoft.EntityFrameworkCore.Tools` | `dotnet ef` migrations CLI |
| `YamlDotNet` | Deserializes `.yaml` files into C# POCOs |
| `builder.Services.AddHttpClient()` | Registers `IHttpClientFactory` (no extra service file) |
