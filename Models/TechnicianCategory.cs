using System;
using System.ComponentModel;

namespace G4AspNet.Models;

public class TechnicianCategory
{
    public string TecnicoId {get;set;}= string.Empty;
    public ApplicationUser Tecnico{get;set;} = null!;

    public int CategoriaId {get;set;}
    public Category Categoria {get;set; }= null!;
}
