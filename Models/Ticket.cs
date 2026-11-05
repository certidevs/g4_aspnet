using System.ComponentModel.DataAnnotations;

namespace G4AspNet.Models;

public class Ticket
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    //Foraneas

    public int ProjectId {get; set;} 
    public Project Project {get; set;} = null!;

    public string CreatedById { get; set; } = string.Empty;
    public ApplicationUser CreatedBy { get; set; } = null!;

    public string? AssignedToId { get; set; }
    public ApplicationUser? AssignedTo { get; set; }


    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public List<TicketComment> Comments { get; set; } = new();
}