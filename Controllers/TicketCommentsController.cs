
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using G4AspNet.Data;
using G4AspNet.Models;

namespace G4AspNet.Controllers;

// Admin, User, Technician y TeamLeader pueden consultar comentarios.
[Authorize(Roles = RoleNames.Admin + "," + RoleNames.User + "," + RoleNames.Technician + "," + RoleNames.TeamLeader)]
public class TicketCommentsController : Controller
{
    private readonly ApplicationDbContext context;

    public TicketCommentsController(ApplicationDbContext context)
    {
        this.context = context;
    }

    // GET: /TicketComments
    // Lista los comentarios que puede consultar el usuario.
    public IActionResult Index()
    {
        var consulta = context.TicketComments
            .Include(c => c.Ticket)
            .Include(c => c.Author)
            .AsQueryable();

        if (!PuedeVerComentariosInternos())
        {
            consulta = consulta.Where(c => !c.IsInternal);
        }

        var comments = consulta
            .OrderByDescending(c => c.CreatedAt)
            .ToList();

        return View(comments);
    }

    // GET: /TicketComments/Details/1
    // Muestra el detalle de un comentario si el usuario puede verlo.
    public IActionResult Details(int id)
    {
        var consulta = context.TicketComments
            .Include(c => c.Ticket)
            .Include(c => c.Author)
            .AsQueryable();

        if (!PuedeVerComentariosInternos())
        {
            consulta = consulta.Where(c => !c.IsInternal);
        }

        var comment = consulta.FirstOrDefault(c => c.Id == id);

        if (comment == null)
        {
            return NotFound();
        }

        return View(comment);
    }

    // GET: /TicketComments/Create?ticketId=1
    // Solo Admin, User y Technician pueden crear comentarios.
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.User + "," + RoleNames.Technician)]
    public async Task<IActionResult> Create(int ticketId)
    {
        var ticketExists = await context.Tickets
            .AnyAsync(t => t.Id == ticketId);

        if (!ticketExists)
        {
            return NotFound();
        }

        return View("Form", new TicketComment { TicketId = ticketId });
    }

    // GET: /TicketComments/Edit/1
    // Solo el autor o un administrador pueden editar.
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.User + "," + RoleNames.Technician)]
    public async Task<IActionResult> Edit(int id)
    {
        var comment = await context.TicketComments.FindAsync(id);

        if (comment == null)
        {
            return NotFound();
        }

        if (!CanManageComment(comment))
        {
            return Forbid();
        }

        return View("Form", comment);
    }

    // POST: /TicketComments/Save
    // Crea o actualiza un comentario.
    [HttpPost]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.User + "," + RoleNames.Technician)]
    public async Task<IActionResult> Save(TicketComment comment)
    {
        // El formulario no envía estos objetos ni el AuthorId.
        ModelState.Remove(nameof(TicketComment.Ticket));
        ModelState.Remove(nameof(TicketComment.Author));
        ModelState.Remove(nameof(TicketComment.AuthorId));

        // Identificamos al usuario que ha iniciado sesión.
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (comment.Id == 0 && userId == null)
        {
            ModelState.AddModelError(
                string.Empty,
                "Tienes que iniciar sesión para escribir un comentario.");
        }

        // Los comentarios internos solo pueden crearlos técnicos y administradores.
        if (comment.IsInternal &&
            !User.IsInRole(RoleNames.Technician) &&
            !User.IsInRole(RoleNames.Admin))
        {
            ModelState.AddModelError(
                nameof(TicketComment.IsInternal),
                "No tienes permiso para crear comentarios internos.");
        }

        if (!ModelState.IsValid)
        {
            return View("Form", comment);
        }

        var isNew = comment.Id == 0;

        if (isNew)
        {
            var ticketExists = await context.Tickets
                .AnyAsync(t => t.Id == comment.TicketId);

            if (!ticketExists)
            {
                return NotFound();
            }

            comment.AuthorId = userId!;
            comment.CreatedAt = DateTime.UtcNow;

            context.TicketComments.Add(comment);
        }
        else
        {
            var existing = await context.TicketComments
                .FindAsync(comment.Id);

            if (existing == null)
            {
                return NotFound();
            }

            if (!CanManageComment(existing))
            {
                return Forbid();
            }

            existing.Message = comment.Message;
            existing.IsInternal = comment.IsInternal;

            // Conservamos el ticket original.
            comment.TicketId = existing.TicketId;
        }

        await context.SaveChangesAsync();

        TempData["Message"] = isNew
            ? "Comentario añadido correctamente."
            : "Comentario actualizado correctamente.";

        return RedirectToAction(
            "Details",
            "Tickets",
            new { id = comment.TicketId });
    }

    // POST: /TicketComments/Delete/1
    // Solo el autor o un administrador pueden eliminar.
    [HttpPost]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.User + "," + RoleNames.Technician)]
    public async Task<IActionResult> Delete(int id)
    {
        var comment = await context.TicketComments.FindAsync(id);

        if (comment == null)
        {
            return NotFound();
        }

        if (!CanManageComment(comment))
        {
            return Forbid();
        }

        context.TicketComments.Remove(comment);

        await context.SaveChangesAsync();

        TempData["Message"] = "Comentario eliminado correctamente.";

        return RedirectToAction(nameof(Index));
    }

    // Comprueba quién puede consultar comentarios internos.
    private bool PuedeVerComentariosInternos()
    {
        return User.IsInRole(RoleNames.Technician)
            || User.IsInRole(RoleNames.TeamLeader)
            || User.IsInRole(RoleNames.Admin);
    }

    // Comprueba si el usuario es el autor o un administrador.
    private bool CanManageComment(TicketComment comment)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return User.IsInRole(RoleNames.Admin)
            || userId == comment.AuthorId;
    }
}
