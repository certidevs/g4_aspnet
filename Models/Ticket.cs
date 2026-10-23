using System.ComponentModel;

namespace G4AspNet.Models;

public class Ticket
{
    public int Id {get; set;}
    public string Titulo {get; set;} = string.Empty;
    public string Descripcion {get; set;} = string.Empty;

    public DateTime FechaCreacion {get; set;} = DateTime.Now;

    public DateTime? FechaResolucion {get; set;}

    public EstadoTicket Estado {get; set;}

    public PrioridadTicket Prioridad {get; set;}



    //Foraneas
    public string UsuarioId {get; set;}=string.Empty;
    public ApplicationUser Usuario {get; set;} = null!;

    public string? ResponsableId {get; set;}
    public ApplicationUser? Responsable {get; set;}

    public int CategoriaId {get; set;}
    public Category Categoria {get; set;} = null!;

}