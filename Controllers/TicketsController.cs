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
    private const string NoAvailableProjectsMessage = "Tu empresa no tiene proyectos activos para crear un ticket.";

    // acceso a la base de datos
    private readonly ApplicationDbContext context;

    public TicketsController(ApplicationDbContext context)
    {
        this.context = context;
    }

    // GET: /Tickets
    public IActionResult Index(string? search)
    {
        // Obtenemos el usuario actual que ha hecho login
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        // Include carga el proyecto, la categoría y el técnico de cada ticket para poder mostrarlos
        var query = context.Tickets
            .Include(t => t.Project)
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
            query = query.Where(t => t.Title.Contains(search)
                || t.Description.Contains(search)
                || t.Project.Name.Contains(search));
        }
        ViewBag.Search = search;

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
        var userId = User.GetRequiredUserId();
        var projects = context.Projects.AsQueryable();

        // Si la empresa del usuario está inactiva, no puede crear un ticket
        if (!User.IsInRole(RoleNames.Admin))
        {
            var activeCompany = await context.Users
                .AnyAsync(u => u.Id == userId && u.Company != null && u.Company.IsActive);

            if (!activeCompany)
            {
                TempData["Error"] = "Tu empresa no está activa.";
                return RedirectToAction(nameof(Index));
            }

            projects = GetCompanyProjects(userId);
            if (!await projects.AnyAsync())
            {
                TempData["Error"] = NoAvailableProjectsMessage;
                return RedirectToAction(nameof(Index));
            }
        }
        // cargar las listas de los selects
        await LoadSelectListsAsync(projects);
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

        if (ticket.Id == 0)
        {
            ModelState.Remove(nameof(Ticket.AssignedToId));
        }

        // Id del usuario logueado
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (ticket.Id == 0 && userId == null)
        {
            ModelState.AddModelError(string.Empty, "Tienes que iniciar sesión para crear un ticket.");
        }
        var projects = context.Projects.AsQueryable();
        // El cliente solo puede crear tickets para proyectos activos de su empresa.
        if (ticket.Id == 0 && !User.IsInRole(RoleNames.Admin))
        {
            projects = GetCompanyProjects(userId);
            if (!await projects.AnyAsync(p => p.Id == ticket.ProjectId))
            {
                if (!await projects.AnyAsync())
                {
                    TempData["Error"] = NoAvailableProjectsMessage;
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError(nameof(Ticket.ProjectId), "Selecciona un proyecto activo de tu empresa.");
            }
        }
        // si no valida, volver al formulario con los errores
        if (!ModelState.IsValid)
        {
            await LoadSelectListsAsync(projects);
            return View("Form", ticket);
        }

        // Id 0 = ticket nuevo
        var isNew = ticket.Id == 0;
        if (isNew)
        {
            // crear: estado Open, fecha y creador los pone el servidor
            ticket.Status = TicketStatus.Open;
            if (!User.IsInRole(RoleNames.Admin))
            {
                ticket.AssignedToId = null; // no asignado al crear
            }

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

            if (ticket.Status == TicketStatus.Closed && existing.Status != TicketStatus.Closed)
            {
                return Forbid();
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

    // POST: /Tickets/Close/1 - Solo el creador puede cerrar el ticket
    [HttpPost]
    [Authorize]
    public Task<IActionResult> Close(int id, string? comment) =>
        ChangeStatusWithCommentAsync(id, comment, TicketStatus.Closed);

    // POST: /Tickets/Reopen/1 - El creador puede reabrir su ticket cerrado
    [HttpPost]
    [Authorize]
    public Task<IActionResult> Reopen(int id, string? comment) =>
        ChangeStatusWithCommentAsync(id, comment, TicketStatus.Open);

    private async Task<IActionResult> ChangeStatusWithCommentAsync(int id, string? comment, TicketStatus newStatus)
    {
        var ticket = await context.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        if (ticket == null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null || ticket.CreatedById != userId)
        {
            return Forbid();
        }

        var isClosing = newStatus == TicketStatus.Closed;
        var isClosed = ticket.Status == TicketStatus.Closed;
        if (isClosing == isClosed)
        {
            return RedirectToAction(nameof(Details), new { id });
        }

        var message = comment?.Trim();
        var maxCommentLength = isClosing ? TicketComment.MaxClosingCommentLength : TicketComment.MaxReopeningCommentLength;
        if (string.IsNullOrEmpty(message) || message.Length > maxCommentLength)
        {
            var action = isClosing ? "cerrar" : "reabrir";
            TempData["Error"] = $"Escribe un comentario de entre 1 y {maxCommentLength} caracteres para {action} el ticket.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var changedAt = DateTime.UtcNow;
        await using var transaction = await context.Database.BeginTransactionAsync();

        // Solo la petición que cambia el estado añade el comentario.
        var updatedTickets = await context.Tickets
            .Where(t => t.Id == id && (isClosing ? t.Status != TicketStatus.Closed : t.Status == TicketStatus.Closed))
            .ExecuteUpdateAsync(update => update
                .SetProperty(t => t.Status, newStatus)
                .SetProperty(t => t.ResolvedAt, t => isClosing ? t.ResolvedAt ?? changedAt : null));

        if (updatedTickets == 0)
        {
            return RedirectToAction(nameof(Details), new { id });
        }

        var messagePrefix = isClosing ? TicketComment.ClosingMessagePrefix : TicketComment.ReopeningMessagePrefix;
        context.TicketComments.Add(new TicketComment
        {
            TicketId = id,
            AuthorId = userId,
            Message = messagePrefix + message,
            CreatedAt = changedAt
        });
        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["Message"] = isClosing ? "Ticket cerrado correctamente." : "Ticket reabierto correctamente.";
        return RedirectToAction(nameof(Details), new { id });
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

    private IQueryable<Project> GetCompanyProjects(string? userId)
    {
        return context.Projects.Where(p => p.IsActive && p.Company.IsActive && p.Company.Users.Any(u => u.Id == userId));
    }

    // rellenar los selects del formulario
    private async Task LoadSelectListsAsync(IQueryable<Project>? availableProjects = null)
    {
        var projects = await (availableProjects ?? context.Projects).OrderBy(p => p.Name).ToListAsync();
        ViewBag.Projects = new SelectList(projects, "Id", "Name");

        var categories = await context.Categories.OrderBy(c => c.Name).ToListAsync();
        ViewBag.Categories = new SelectList(categories, "Id", "Name");

        // técnico: de momento cualquier usuario
        var users = await context.Users.OrderBy(u => u.UserName).ToListAsync();
        ViewBag.Technicians = new SelectList(users, "Id", "UserName");
    }
}
