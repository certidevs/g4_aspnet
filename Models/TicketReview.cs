using System.Data;

namespace G4AspNet.Models;

public class TicketReview
{
    public int Id { get; set; }
    public string UsuarioId { get; set; } = string.Empty;
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
    public ApplicationUser Usuario { get; set; } = null!;
    public int Puntuacion { get; set; }
    public string Comentario { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
}