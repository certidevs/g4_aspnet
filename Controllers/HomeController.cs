using G4AspNet.Data;
using G4AspNet.Models;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace G4AspNet.Controllers;

/// <summary>Página de inicio del centro de ayuda: resumen de tickets y niveles de soporte.</summary>
public sealed class HomeController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index()
    {
        // Solo cifras agregadas: la portada es pública y no debe mostrar tickets de nadie.
        ViewData["OpenTickets"] = await context.Tickets.CountAsync(t => t.Status == TicketStatus.Open);
        ViewData["InProgressTickets"] = await context.Tickets.CountAsync(t => t.Status == TicketStatus.InProgress || t.Status == TicketStatus.WaitingCustomer);
        ViewData["ResolvedTickets"] = await context.Tickets.CountAsync(t => t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed);
        ViewData["Companies"] = await context.Companies.CountAsync();

        // Proyectos activos por nivel de soporte y el mejor tiempo de respuesta de cada nivel.
        var levels = new List<SupportSummary>();
        foreach (var level in new[] { SupportLevel.Basic, SupportLevel.Standard, SupportLevel.Premium })
        {
            var projects = context.Projects.Where(p => p.IsActive && p.SupportLevel == level);
            levels.Add(new SupportSummary(
                level,
                await projects.CountAsync(),
                await projects.Select(p => (int?)p.ResponseTimeHours).MinAsync()));
        }
        ViewData["SupportLevels"] = levels;

        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
    });

    /// <summary>Nivel de soporte, proyectos activos en ese nivel y mejor tiempo de respuesta en horas.</summary>
    public sealed record SupportSummary(SupportLevel Level, int Projects, int? BestResponseHours);
}
