using Microsoft.AspNetCore.Identity;
namespace G4AspNet.Models;

public sealed class ApplicationUser : IdentityUser
{
    /// <summary>Nombre que se enseña en la interfaz; puede ser distinto al login.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Nombre aleatorio del avatar guardado en wwwroot/uploads/avatars.</summary>
    public string? AvatarFileName { get; set; }

    /// <summary>Fecha de alta de la cuenta.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Permite desactivar una cuenta sin borrar la información futura que posea.</summary>
    public bool IsActive { get; set; } = true;

    // Empresa del usuario: opcional (int?), el administrador no pertenece a ninguna empresa cliente.
    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    // Colecciones inversas de las relaciones de Ticket y TicketComment con el usuario.
    public ICollection<Ticket> CreatedTickets { get; set; } = new List<Ticket>();
    public ICollection<Ticket> AssignedTickets { get; set; } = new List<Ticket>();
    public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
}
