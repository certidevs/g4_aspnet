using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims; // lo necesita el bloque comentado de abajo (ClaimTypes)
using Microsoft.AspNetCore.Mvc.Rendering; // lo necesita el bloque comentado de abajo (SelectList)
using G4AspNet.Data;
using G4AspNet.Models; // lo necesita el bloque comentado de abajo (Ticket, TicketStatus)
namespace G4AspNet.Controllers;

public class TicketsController : Controller
{
    private readonly ApplicationDbContext context;

    public TicketsController(ApplicationDbContext context)
    {
        this.context = context;
    }

    // GET: /Tickets
    public IActionResult Index()
    {
        // Include carga el proyecto, la categoría y el técnico de cada ticket para poder mostrarlos
        var tickets = context.Tickets
            .Include(t => t.Project)
            .Include(t => t.Category)
            .Include(t => t.AssignedTo)
            .OrderByDescending(t => t.CreatedAt)
            .ToList();
        return View(tickets);
    }

    // GET: /Tickets/Details/1
    public IActionResult Details(int id)
    {
        // Incluimos todas las relaciones que se muestran: proyecto (con su empresa), categoría,
        // usuarios y los comentarios del ticket con el autor de cada uno
        var ticket = context.Tickets
            .Include(t => t.Project)
                .ThenInclude(p => p.Company)
            .Include(t => t.Category)
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .Include(t => t.Comments)
                .ThenInclude(c => c.Author)
            .FirstOrDefault(t => t.Id == id);
        if (ticket == null)
        {
            return NotFound();
        }

        return View(ticket);
    }

/* DESCOMENTA DESDE AQUÍ (issue #16): Create, Edit, Save y Delete
   // Pasos: 1) quita este comentario (borra la primera línea y la última, FIN DESCOMENTA), 2) ajusta los campos de tu entidad, 3) descomenta y ajusta Views/Tickets/Form.cshtml, 4) descomenta los botones de Index y Details.
   // Modelo a copiar: CompaniesController (ejemplo completo y probado).
   // Los comentarios del ticket se añaden desde Details con el botón "Añadir comentario" (TicketCommentsController.Create).

    // GET: /Tickets/Create
    // Muestra el formulario vacío. Crear y Editar comparten la vista Views/Tickets/Form.cshtml
    public async Task<IActionResult> Create()
    {
        // El formulario necesita las listas de proyectos, categorías y técnicos para sus selects
        await LoadSelectListsAsync();
        return View("Form", new Ticket());
    }

    // GET: /Tickets/Edit/1
    // Muestra el mismo formulario, relleno con los datos del ticket
    public async Task<IActionResult> Edit(int id)
    {
        // FindAsync busca por clave primaria y devuelve null si no existe
        var ticket = await context.Tickets.FindAsync(id);
        if (ticket == null)
        {
            return NotFound();
        }

        await LoadSelectListsAsync();
        return View("Form", ticket);
    }

    // POST: /Tickets/Save
    // Crea o actualiza según el Id: 0 = ticket nuevo, otro valor = ticket que ya existe
    [HttpPost]
    public async Task<IActionResult> Save(Ticket ticket)
    {
        // El formulario solo envía ProjectId y CategoryId (los selects), no los objetos Project y Category,
        // y CreatedById lo pone el servidor. Como en el modelo no son nulos, MVC los marcaría como "obligatorios"
        // y el formulario nunca validaría: quitamos esas validaciones.
        ModelState.Remove(nameof(Ticket.Project));
        ModelState.Remove(nameof(Ticket.Category));
        ModelState.Remove(nameof(Ticket.CreatedBy));
        ModelState.Remove(nameof(Ticket.CreatedById));

        // Quién crea el ticket: el usuario que ha iniciado sesión (su Id está en la cookie de sesión)
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (ticket.Id == 0 && userId == null)
        {
            ModelState.AddModelError(string.Empty, "Tienes que iniciar sesión para crear un ticket.");
        }

        // Si algo no valida, volvemos al formulario con los errores
        if (!ModelState.IsValid)
        {
            // Hay que volver a cargar los selects, porque las listas no viajan en el formulario
            await LoadSelectListsAsync();
            return View("Form", ticket);
        }

        var isNew = ticket.Id == 0;
        if (isNew)
        {
            // CREAR: estos datos no vienen del formulario, los pone el servidor
            ticket.Status = TicketStatus.Open;
            ticket.CreatedAt = DateTime.UtcNow;
            ticket.CreatedById = userId!; // no es null: arriba hemos comprobado que hay sesión iniciada
            context.Tickets.Add(ticket);
        }
        else
        {
            // EDITAR: cargamos el ticket y le copiamos solo los campos del formulario
            // (así CreatedAt, CreatedById y ResolvedAt no se pierden)
            var existing = await context.Tickets.FindAsync(ticket.Id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.Title = ticket.Title;
            existing.Description = ticket.Description;
            existing.Priority = ticket.Priority;
            existing.Status = ticket.Status;
            existing.ProjectId = ticket.ProjectId;
            existing.CategoryId = ticket.CategoryId;
            existing.AssignedToId = ticket.AssignedToId;

            // ResolvedAt se rellena al resolver o cerrar el ticket, y se vacía si se vuelve a abrir
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

        await context.SaveChangesAsync();

        // TempData guarda un mensaje para la siguiente petición; el _Layout lo muestra
        TempData["Message"] = isNew ? "Ticket creado correctamente." : "Ticket actualizado correctamente.";

        // Vamos al detalle del ticket, que es donde se añaden comentarios
        return RedirectToAction(nameof(Details), new { id = ticket.Id });
    }

    // POST: /Tickets/Delete/1
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var ticket = await context.Tickets.FindAsync(id);
        if (ticket == null)
        {
            return NotFound();
        }

        // La relación Comentario -> Ticket es Cascade: al borrar el ticket, la base de datos borra también
        // sus comentarios. Un ticket no tiene nada que lo bloquee, así que no hace falta comprobar dependientes.
        context.Tickets.Remove(ticket);
        await context.SaveChangesAsync();

        TempData["Message"] = "Ticket eliminado correctamente (y sus comentarios).";
        return RedirectToAction(nameof(Index));
    }

    // Rellena los selects del formulario. SelectList(lista, campo del valor, campo del texto)
    private async Task LoadSelectListsAsync()
    {
        var projects = await context.Projects.OrderBy(p => p.Name).ToListAsync();
        ViewBag.Projects = new SelectList(projects, "Id", "Name");

        var categories = await context.Categories.OrderBy(c => c.Name).ToListAsync();
        ViewBag.Categories = new SelectList(categories, "Id", "Name");

        // Técnico: de momento sirve cualquier usuario (cuando se explique la seguridad se filtrará por rol)
        var users = await context.Users.OrderBy(u => u.UserName).ToListAsync();
        ViewBag.Technicians = new SelectList(users, "Id", "UserName");
    }
FIN DESCOMENTA */

}
