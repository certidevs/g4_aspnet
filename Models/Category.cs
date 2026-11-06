using System.ComponentModel.DataAnnotations;

namespace G4AspNet.Models;

public class Category
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    // OPCIONAL: con `string?` el formulario deja guardar la descripción vacía.
    // Con `string` (sin ?) MVC la exige aunque no lleve [Required], porque el proyecto usa nulabilidad.
    [StringLength(2000)]
    public string? Description { get; set; }

    // Colección inversa de Ticket.Category. Tiene que ser una PROPIEDAD ({ get; set; }):
    // EF Core no mapea los campos públicos, y así la relación no se cargaría nunca.
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
