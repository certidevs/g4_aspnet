using System.ComponentModel.DataAnnotations;
namespace G4AspNet.Models;

public class Project
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    // OPCIONAL: con `string?` el formulario deja guardar la descripción vacía.
    // Con `string` (sin ?) MVC la exige aunque no lleve [Required], porque el proyecto usa nulabilidad.
    [StringLength(500)]
    public string? Description { get; set; }
    public SupportLevel SupportLevel { get; set; }

    [Range(1, 720)]
    public int ResponseTimeHours { get; set; }
    public bool IsActive { get; set; } = true;

    // Clave foránea obligatoria (int) con su navegación obligatoria (no nula), como en Ticket.
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
