using ApiMonitor.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiMonitor.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var total   = await _db.Apis.CountAsync();
        var running = await _db.Apis.CountAsync(a => a.IsRunning);
        var down    = total - running;

        ViewBag.Total   = total;
        ViewBag.Running = running;
        ViewBag.Down    = down;

        return View();
    }
}
