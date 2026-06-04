using Looma.Domain.Core;
using Looma.Domain.Entities;

namespace Looma.Presentation.ViewModels.Sections.Patterns;

public record PatternProjectViewModel(PatternProject Project)
{
    public string StatusDisplay => Project.Status switch
    {
        Status.Wishlist => "Wishlist",
        Status.InProgress => "En cours",
        Status.Finished => "Terminé",
        _ => Project.Status.ToString()
    };
}
