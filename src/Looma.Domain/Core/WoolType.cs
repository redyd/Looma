using System.ComponentModel.DataAnnotations;

namespace Looma.Domain.Core;

public enum WoolType
{
    [Display(Name = "Dentelle")]
    Lace,
    [Display(Name = "Super fin")]
    SuperFine,
    [Display(Name = "Fin")]
    Fine,
    [Display(Name = "Léger")]
    Light,
    [Display(Name = "Moyen")]
    Medium,
    [Display(Name = "Bulky")]
    Bulky,
    [Display(Name = "Très épais")]
    SuperBulky,
    [Display(Name = "Géant")]
    Jumbo
}