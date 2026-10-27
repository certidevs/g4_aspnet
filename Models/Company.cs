using System;
using System.ComponentModel.DataAnnotations;

namespace G4AspNet.Models;

public class Company
{
    public int Id {get; set;}
    
    public string Name {get; set;} = string.Empty;


    public string TaxId {get; set;} = string.Empty;
    // CIF número de identificador de la empresa
    public string Email {get; set;} = string.Empty;

    public string Phone {get; set;} = string.Empty;

    public bool IsActive {get; set;}
}
