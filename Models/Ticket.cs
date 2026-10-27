using System.ComponentModel;

namespace G4AspNet.Models;

public class Ticket
{
    public int Id {get; set;}
    public string Title {get; set;} = string.Empty;
    public string Descripcion {get; set;} = string.Empty;

    public DateTime CreatedAt {get; set;} = DateTime.Now;

    public DateTime? ResolvedAt {get; set;}

    public EstadoTicket Estado {get; set;}

    public PrioridadTicket Prioridad {get; set;}



    //Foraneas
    
    public string UsuarioId {get; set;}=string.Empty;
    public ApplicationUser Usuario {get; set;} = null!;

    public string? ResponsableId {get; set;}
    public ApplicationUser? Responsable {get; set;}

    public int CategoriaId {get; set;}
    public Category Categoria {get; set;} = null!;

    
    // public ICollection<TicketComment> Comentarios {get; set;} = new List<TicketComment>();
    // public ICollection<TicketHistory> Historial {get; set;} = new List<TicketHistory>();
    // public TicketReview? Valoracion {get; set;}

}