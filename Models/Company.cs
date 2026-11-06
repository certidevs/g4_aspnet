using System.ComponentModel.DataAnnotations;

namespace G4AspNet.Models;

public class Company
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    // CIF: número de identificación fiscal de la empresa (único, ver ApplicationDbContext).
    [Required]
    [StringLength(20)]
    public string TaxId { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(30)]
    public string Phone { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    // Colecciones inversas: proyectos de la empresa y usuarios (clientes) que trabajan en ella.
    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
}
