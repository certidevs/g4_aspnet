using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims; // lo necesita el bloque comentado de abajo (ClaimTypes)
using G4AspNet.Data;
using G4AspNet.Models; // lo necesita el bloque comentado de abajo (TicketComment)
namespace G4AspNet.Controllers;

public class TicketCommentsController : Controller
{
    private readonly ApplicationDbContext context;

    public TicketCommentsController(ApplicationDbContext context)
    {
        this.context = context;
    }

    // GET: /TicketComments
    public IActionResult Index()
    {
        // Include carga el ticket y el autor de cada comentario para poder mostrarlos
        var comments = context.TicketComments
            .Include(c => c.Ticket)
            .Include(c => c.Author)
            .OrderByDescending(c => c.CreatedAt)
            .ToList();
        return View(comments);
    }

    // GET: /TicketComments/Details/1
    public IActionResult Details(int id)
    {
        var comment = context.TicketComments
            .Include(c => c.Ticket)
            .Include(c => c.Author)
            .FirstOrDefault(c => c.Id == id);
        if (comment == null)
        {
            return NotFound();
        }

        return View(comment);
    }

// DESCOMENTA DESDE AQUÍ (issue #18): Create, Edit, Save y Delete
   // Pasos: 1) quita este comentario (borra la primera línea y la última, FIN DESCOMENTA), 2) ajusta los campos de tu entidad, 3) descomenta y ajusta Views/TicketComments/Form.cshtml, 4) descomenta los botones de Index y Details (y el botón "Añadir comentario" de Views/Tickets/Details.cshtml).
   // Modelo a copiar: CompaniesController (ejemplo completo y probado).
   // Los comentarios se crean desde el detalle de un ticket: el botón lleva a Create con el ticketId.

    // GET: /TicketComments/Create?ticketId=1
    // Muestra el formulario vacío para comentar el ticket indicado. Crear y Editar comparten la vista Form.
    public async Task<IActionResult> Create(int ticketId)
    {
        // Comprobamos que el ticket existe antes de enseñar el formulario
        var ticketExists = await context.Tickets.AnyAsync(t => t.Id == ticketId);
        if (!ticketExists)
        {
            return NotFound();
        }

        return View("Form", new TicketComment { TicketId = ticketId });
    }

    // GET: /TicketComments/Edit/1
    // Muestra el mismo formulario, relleno con los datos del comentario
    /* public async Task<IActionResult> Edit(int id)
    {
        // FindAsync busca por clave primaria y devuelve null si no existe
        var comment = await context.TicketComments.FindAsync(id);
        if (comment == null)
        {
            return NotFound();
        }

        return View("Form", comment);
    } */

    public async Task<IActionResult> Edit(int id)
{
    // Buscamos el comentario que se quiere editar.
    var comment = await context.TicketComments.FindAsync(id);

    if (comment == null)
    {
        return NotFound();
    }

    // Solo el autor o un administrador pueden editarlo.
    if (!CanManageComment(comment))
    {
        return Forbid();
    }

    return View("Form", comment);
}

    // POST: /TicketComments/Save
    // Crea o actualiza según el Id: 0 = comentario nuevo, otro valor = comentario que ya existe
    [HttpPost]
    public async Task<IActionResult> Save(TicketComment comment)
    {
        // El formulario solo envía TicketId, Message e IsInternal. Los objetos Ticket y Author y el AuthorId
        // no vienen del formulario; como en el modelo no son nulos, MVC los marcaría como "obligatorios"
        // y el formulario nunca validaría: quitamos esas validaciones.
        ModelState.Remove(nameof(TicketComment.Ticket));
        ModelState.Remove(nameof(TicketComment.Author));
        ModelState.Remove(nameof(TicketComment.AuthorId));

        // Autor del comentario: el usuario que ha iniciado sesión (su Id está en la cookie de sesión)
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (comment.Id == 0 && userId == null)
        {
            ModelState.AddModelError(string.Empty, "Tienes que iniciar sesión para escribir un comentario.");
        }

        // Si algo no valida, volvemos al formulario con los errores
        if (!ModelState.IsValid)
        {
            return View("Form", comment);
        }

        var isNew = comment.Id == 0;
        if (isNew)
        {
            // CREAR: comprobamos que el ticket existe (el TicketId viaja en un campo oculto)
            var ticketExists = await context.Tickets.AnyAsync(t => t.Id == comment.TicketId);
            if (!ticketExists)
            {
                return NotFound();
            }

            // Estos datos no vienen del formulario, los pone el servidor
            comment.AuthorId = userId!; // no es null: arriba hemos comprobado que hay sesión iniciada
            comment.CreatedAt = DateTime.UtcNow;
            context.TicketComments.Add(comment);
        }
        else
        {
            // EDITAR: cargamos el comentario y solo cambiamos el mensaje y si es interno
            // (el ticket, el autor y la fecha no se modifican)
            var existing = await context.TicketComments.FindAsync(comment.Id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.Message = comment.Message;
            existing.IsInternal = comment.IsInternal;
            comment.TicketId = existing.TicketId; // para saber a qué ticket volver
        }

        await context.SaveChangesAsync();

        // TempData guarda un mensaje para la siguiente petición; el _Layout lo muestra
        TempData["Message"] = isNew ? "Comentario añadido correctamente." : "Comentario actualizado correctamente.";

        // Volvemos al detalle del ticket, que es donde se ven los comentarios
        return RedirectToAction("Details", "Tickets", new { id = comment.TicketId });
    }

    // POST: /TicketComments/Delete/1
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var comment = await context.TicketComments.FindAsync(id);
        if (comment == null)
        {
            return NotFound();
        }

        // Un comentario no tiene nada que dependa de él, así que se puede borrar sin más comprobaciones
        context.TicketComments.Remove(comment);
        await context.SaveChangesAsync();

        TempData["Message"] = "Comentario eliminado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // Comprueba si el usuario actual es el autor del comentario o un administrador.
    private bool CanManageComment(TicketComment comment)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return User.IsInRole(RoleNames.Admin) || userId == comment.AuthorId;
    }

}
