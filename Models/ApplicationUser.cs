using Microsoft.AspNetCore.Identity;
namespace G4AspNet.Models;

public class ApplicationUser : IdentityUser
{
    public string Nombre {set; get;} = string.Empty;
    public string Apellidos {set; get;} = string.Empty;
    public DateTime FechaRegistro {get; set;} = DateTime.Now;
    public Rol Rol;
}
