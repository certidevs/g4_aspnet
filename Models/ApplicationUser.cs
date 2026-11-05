using Microsoft.AspNetCore.Identity;
namespace G4AspNet.Models;

public sealed class ApplicationUser : IdentityUser
{
    /// <summary>Nombre que se enseña en la interfaz; puede ser distinto al login.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Nombre aleatorio del avatar guardado en wwwroot/uploads/avatars.</summary>
    public string? AvatarFileName { get; set; }

    /// <summary>Fecha de alta de la cuenta.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Permite desactivar una cuenta sin borrar la información futura que posea.</summary>
    public bool IsActive { get; set; } = true;
    public Company? Company {get; set;}
    public int? CompanyId {get; set;}
}
