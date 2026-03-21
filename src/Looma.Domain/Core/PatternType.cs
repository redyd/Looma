using System.ComponentModel.DataAnnotations;

namespace Looma.Domain.Core;

public enum PatternType
{
    [Display(Name = "Crochet")]
    Crochet = 0,
    [Display(Name = "Crochet tunisien")]
    TunisianCrochet = 1,
    [Display(Name = "Tricot")]
    Tricot = 2
}