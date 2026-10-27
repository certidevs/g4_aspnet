using System.ComponentModel.DataAnnotations;

namespace G4AspNet.Models;

public enum EstadoTicket
{
    [Display (Name = "Abierto")]
    Open,
    [Display (Name = "En progreso")]
    InProgress,
    [Display (Name = "Espera de cliente")]
    WaitingCustomer,
    [Display (Name = "Resuelto")]
    Resolved,
    [Display (Name = "Cerrado")]
    Closed
}