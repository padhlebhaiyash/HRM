using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using HRMSystem.Models;

namespace HRMSystem.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
        {
            var sessionRoles = HttpContext.Session.GetString("UserRoles") ?? "";
            var portal = HttpContext.Session.GetString("LoginPortal") ?? "Employee";

            if (portal == "Employee")
                return RedirectToAction("Dashboard", "EmployeeSelf");
            else if (sessionRoles.Contains("Admin"))
                return RedirectToAction("Dashboard", "Admin");
            else
                return RedirectToAction("Dashboard", "Manager");
        }
        
        return RedirectToAction("Login", "Auth");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
