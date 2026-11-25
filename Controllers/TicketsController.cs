using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;
using G4AspNet.Data;
using G4AspNet.Models;
using G4AspNet.Utilities; // lo necesita el bloque comentado de abajo (Ticket, TicketStatus)
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
    public IActionResult Index(string? search, TicketStatus? status, TicketPriority? priority,
        int? companyId, int? projectId, int? categoryId, DateTime? from, DateTime? to)
    {
        // Obtenemos el usuario actual que ha hecho login
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        // Include carga el proyecto, la categoría y el técnico de cada ticket para poder mostrarlos
        var query = context.Tickets
            .Include(t => t.Project)
                .ThenInclude(p => p.Company)
            .Include(t => t.Category)
            .Include(t => t.AssignedTo)
            .Include(t => t.CreatedBy)
            .ThenInclude(C => C.Company)
            .AsQueryable();

        // admin: todos; técnico: los que tiene asignados; cliente: los que ha creado
        if (User.IsInRole(RoleNames.Technician))
        {
            query = query.Where(t => t.AssignedToId == userId);
        }
        else if (!User.IsInRole(RoleNames.Admin))
        {
            query = query.Where(t => t.CreatedById == userId);
        }

        // búsqueda por título, descripción o nombre del proyecto
        if (!string.IsNullOrWhiteSpace(search))
        {
            // el admin también puede buscar por Id (con o sin '#')
            var isAdmin = User.IsInRole(RoleNames.Admin);
            var hasId = int.TryParse(search.TrimStart('#'), out var searchId);
            query = query.Where(t => t.Title.Contains(search)
                || t.Description.Contains(search)
                || t.Project.Name.Contains(search)
                || (isAdmin && hasId && t.Id == searchId));
        }
        ViewBag.Search = search;

        // filtros para el administrador
        if (User.IsInRole(RoleNames.Admin))
        {
            if (status != null) query = query.Where(t => t.Status == status);
            if (priority != null) query = query.Where(t => t.Priority == priority);
            if (companyId != null) query = query.Where(t => t.Project.CompanyId == companyId);
            if (projectId != null) query = query.Where(t => t.ProjectId == projectId);
            if (categoryId != null) query = query.Where(t => t.CategoryId == categoryId);
            if (from != null) query = query.Where(t => t.CreatedAt >= from.Value.Date);
            if (to != null) query = query.Where(t => t.CreatedAt < to.Value.Date.AddDays(1));

            // valores de los filtros
            ViewBag.Status = status;
            ViewBag.Priority = priority;
            ViewBag.CompanyId = companyId;
            ViewBag.ProjectId = projectId;
            ViewBag.CategoryId = categoryId;
            ViewBag.From = from;
            ViewBag.To = to;
            ViewBag.HasFilters = status != null || priority != null || companyId != null || projectId != null
                || categoryId != null || from != null || to != null;

            // opciones de listas desplegables
            ViewBag.Companies = new SelectList(context.Companies.OrderBy(c => c.Name), "Id", "Name", companyId);
            ViewBag.Projects = new SelectList(context.Projects.OrderBy(p => p.Name), "Id", "Name", projectId);
            ViewBag.Categories = new SelectList(context.Categories.OrderBy(c => c.Name), "Id", "Name", categoryId);
        }

        var tickets = query
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

    // GET: /Tickets/Create - Solo usuarios y admin pueden crear tickets
    [Authorize(Roles = RoleNames.User + "," + RoleNames.Admin)]
    public async Task<IActionResult> Create()
    {
        // cargar las listas de los selects
        await LoadSelectListsAsync();
        // mismo formulario que Edit, con un ticket vacío
        return View("Form", new Ticket());
    }

    // GET: /Tickets/Edit/1 - Solo admin y técnico pueden editar tickets
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.Technician)]
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

    // POST: /Tickets/Save - Solo usuarios, admin y técnico pueden guardar tickets
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Save(Ticket ticket)
    {
        // crear: cliente o admin; editar: admin o técnico
        var canSave = ticket.Id == 0
            ? User.IsInRole(RoleNames.User) || User.IsInRole(RoleNames.Admin)
            : User.IsInRole(RoleNames.Admin) || User.IsInRole(RoleNames.Technician);
        if (!canSave)
        {
            return Forbid();
        }

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

    // POST: /Tickets/Delete/1 - Solo admin puede eliminar tickets
    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
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
