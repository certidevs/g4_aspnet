using System;

namespace G4AspNet.Models;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SupportLevel SupportLevel { get; set; }
    public int ResponseTimeHours { get; set; }
    public bool IsActive { get; set; }
    public int CompanyId { get; set; }
    public Company? Company { get; set; }
}
