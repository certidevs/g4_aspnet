using System.ComponentModel;

namespace G4AspNet.Models;

public class Ticket
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketStatus Status { get; set; }
    public TicketPriority Priority { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
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
}