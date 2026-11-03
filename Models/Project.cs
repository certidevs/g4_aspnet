using System.ComponentModel.DataAnnotations;
namespace G4AspNet.Models;

public class Project
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    public SupportLevel SupportLevel { get; set; }

    [Range(1, 720)]
    public int ResponseTimeHours { get; set; }
    public bool IsActive { get; set; } = true;
    public int CompanyId { get; set; }
    public Company? Company { get; set; }
    public List<Ticket> Tickets { get; set; } = new();
}
