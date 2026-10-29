using System.ComponentModel.DataAnnotations;
namespace G4AspNet.Models;

public enum TicketPriority
{
    [Display (Name = "Bajo")]
    Low,
    [Display (Name = "Medio")]
    Medium,
    [Display (Name = "Alto")]
    High,
    [Display (Name = "Urgente")]
    Urgent
}
