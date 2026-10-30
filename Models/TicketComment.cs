using System.ComponentModel.DataAnnotations;

namespace G4AspNet.Models;

/// <summary>
/// Representa un comentario asociado a un ticket.
/// </summary>
public class TicketComment
{
    // 1.-  ID-PK
    public int Id { get; set; }

    // 2.-  TICKETID-FK
    // Ticket al que pertenece.
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    // 3.-  AUTORID-FK
    // Usuario que lo ha escrito.
    public string AuthorId { get; set; } = string.Empty;
    public ApplicationUser Author { get; set; } = null!;

    // 4.-  MESSAGE
    [Required]
    [StringLength(2000)]
    public string Message { get; set; } = string.Empty;

    // 5.-  ISINTERNAL
    // Indica si el comentario es interno.
    public bool IsInternal { get; set; }

    // 6.-  CREATEAT
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
