using ApiMonitor.Data;
using ApiMonitor.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------------
// Services
// -------------------------------------------------------
builder.Services.AddControllersWithViews();

// EF Core → SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// HttpClient — used by ApiConnectionService (no separate service file needed)
builder.Services.AddHttpClient();

// App services
builder.Services.AddScoped<YamlLoaderService>();
builder.Services.AddScoped<ApiConnectionService>();
builder.Services.AddScoped<ApiSyncService>();

// -------------------------------------------------------
var app = builder.Build();
// -------------------------------------------------------

// Auto-migrate and sync YAML on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // EnsureCreated creates all tables directly from the model if they don't exist.
    // When you're ready for proper migrations, delete the DB, run:
    //   dotnet ef migrations add InitialCreate
    //   dotnet ef database update
    // then swap this back to db.Database.Migrate()
    db.Database.EnsureCreated();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
