using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;
using G4AspNet.Data;
using G4AspNet.Models;
namespace G4AspNet.Controllers;

public class TicketsController : Controller
{
    // acceso a la base de datos
    private readonly ApplicationDbContext context;

    public TicketsController(ApplicationDbContext context)
    {
        this.context = context;
    }

    // GET: /Tickets
    public IActionResult Index()
    {
        // preparar la query con las relaciones (Include) y ordenar por fecha
        var tickets = context.Tickets
            .Include(t => t.Project)
            .Include(t => t.Category)
            .Include(t => t.AssignedTo)
            .OrderByDescending(t => t.CreatedAt)
            .ToList(); // ejecutar la query
        return View(tickets);
    }

    // GET: /Tickets/Details/1
    public IActionResult Details(int id)
    {
        // buscar el ticket con todo lo que muestra la vista
        var ticket = context.Tickets
            .Include(t => t.Project)
                .ThenInclude(p => p.Company)
            .Include(t => t.Category)
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .Include(t => t.Comments)
                .ThenInclude(c => c.Author)
            .FirstOrDefault(t => t.Id == id);

        // si no existe, 404
        if (ticket == null)
        {
            return NotFound();
        }

        return View(ticket);
    }

    // GET: /Tickets/Create
    public async Task<IActionResult> Create()
    {
        // cargar las listas de los selects
        await LoadSelectListsAsync();
        // mismo formulario que Edit, con un ticket vacío
        return View("Form", new Ticket());
    }

    // GET: /Tickets/Edit/1
    public async Task<IActionResult> Edit(int id)
    {
        // buscar el ticket por Id
        var ticket = await context.Tickets.FindAsync(id);
        if (ticket == null)
        {
            return NotFound();
        }

        await LoadSelectListsAsync();
        // mismo formulario, relleno con el ticket
        return View("Form", ticket);
    }

    // POST: /Tickets/Save
    [HttpPost]
    public async Task<IActionResult> Save(Ticket ticket)
    {
        // el formulario no envía estos campos: quitarlos de la validación
        ModelState.Remove(nameof(Ticket.Project));
        ModelState.Remove(nameof(Ticket.Category));
        ModelState.Remove(nameof(Ticket.CreatedBy));
        ModelState.Remove(nameof(Ticket.CreatedById));

        // Id del usuario logueado
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (ticket.Id == 0 && userId == null)
        {
            ModelState.AddModelError(string.Empty, "Tienes que iniciar sesión para crear un ticket.");
        }

        // si no valida, volver al formulario con los errores
        if (!ModelState.IsValid)
        {
            await LoadSelectListsAsync();
            return View("Form", ticket);
        }

        // Id 0 = ticket nuevo
        var isNew = ticket.Id == 0;
        if (isNew)
        {
            // crear: estado Open, fecha y creador los pone el servidor
            ticket.Status = TicketStatus.Open;
            ticket.CreatedAt = DateTime.UtcNow;
            ticket.CreatedById = userId!;
            context.Tickets.Add(ticket);
        }
        else
        {
            // editar: buscar el ticket existente
            var existing = await context.Tickets.FindAsync(ticket.Id);
            if (existing == null)
            {
                return NotFound();
            }

            // copiar solo los campos del formulario
            existing.Title = ticket.Title;
            existing.Description = ticket.Description;
            existing.Priority = ticket.Priority;
            existing.Status = ticket.Status;
            existing.ProjectId = ticket.ProjectId;
            existing.CategoryId = ticket.CategoryId;
            existing.AssignedToId = ticket.AssignedToId;

            // ResolvedAt: se rellena al resolver/cerrar, se vacía si se reabre
            var isFinished = existing.Status == TicketStatus.Resolved || existing.Status == TicketStatus.Closed;
            if (isFinished && existing.ResolvedAt == null)
            {
                existing.ResolvedAt = DateTime.UtcNow;
            }
            else if (!isFinished)
            {
                existing.ResolvedAt = null;
            }
        }

        // guardar en la base de datos
        await context.SaveChangesAsync();

        // mensaje para la siguiente página
        TempData["Message"] = isNew ? "Ticket creado correctamente." : "Ticket actualizado correctamente.";

        // ir al detalle del ticket
        return RedirectToAction(nameof(Details), new { id = ticket.Id });
    }

    // POST: /Tickets/Delete/1
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        // buscar el ticket
        var ticket = await context.Tickets.FindAsync(id);
        if (ticket == null)
        {
            return NotFound();
        }

        // borrar (sus comentarios se borran solos por Cascade)
        context.Tickets.Remove(ticket);
        await context.SaveChangesAsync();

        TempData["Message"] = "Ticket eliminado correctamente (y sus comentarios).";
        return RedirectToAction(nameof(Index));
    }

    // rellenar los selects del formulario
    private async Task LoadSelectListsAsync()
    {
        var projects = await context.Projects.OrderBy(p => p.Name).ToListAsync();
        ViewBag.Projects = new SelectList(projects, "Id", "Name");

        var categories = await context.Categories.OrderBy(c => c.Name).ToListAsync();
        ViewBag.Categories = new SelectList(categories, "Id", "Name");

        // técnico: de momento cualquier usuario
        var users = await context.Users.OrderBy(u => u.UserName).ToListAsync();
        ViewBag.Technicians = new SelectList(users, "Id", "UserName");
    }
}
