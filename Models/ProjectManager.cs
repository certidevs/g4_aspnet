namespace G4AspNet.Models;

/// <summary>
/// Relación jefe de equipo con  proyecto que tenga asignado para gestionar.
/// </summary>
public class ProjectManager
{
    public string ApplicationUserId { get; set; } = string.Empty;
    public ApplicationUser ApplicationUser { get; set; } = null!;

    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
}
