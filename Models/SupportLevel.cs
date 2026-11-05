using System;
using System.ComponentModel.DataAnnotations;

namespace G4AspNet.Models;

public enum SupportLevel
{
    [Display(Name = "Básico")]
    Basic,

    [Display(Name = "Estándar")]
    Standard,

    [Display(Name = "Premium")]
    Premium
}
