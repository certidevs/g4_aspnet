using System.ComponentModel.DataAnnotations;
namespace G4AspNet.Models;

public enum TicketPriority
{
    [Display (Name = "Baja")]
    Low,
    [Display (Name = "Media")]
    Medium,
    [Display (Name = "Alta")]
    High,
    [Display (Name = "Urgente")]
    Urgent
}
