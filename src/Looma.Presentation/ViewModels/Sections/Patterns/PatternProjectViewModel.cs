using Looma.Domain.Entities;

namespace Looma.Presentation.ViewModels.Sections.Patterns;

public record PatternProjectViewModel(PatternProject Project)
{
    public string StatusDisplay => Project.Status switch
    {
        Looma.Domain.Core.Status.Wishlist => "Wishlist",
        Looma.Domain.Core.Status.InProgress => "En cours",
        Looma.Domain.Core.Status.Finished => "Terminé",
        _ => Project.Status.ToString()
    };
}
