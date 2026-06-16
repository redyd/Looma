using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Looma.Presentation.ViewModels.Shared.Patterns;

public partial class PatternProjectViewModel : ObservableObject
{
    [ObservableProperty] public partial string Name { get; set; } = "Aucun nom";
    [ObservableProperty] public partial string StatusDisplay { get; set; } = "Aucun status";
    public ICommand? OpenCommand { get; init; }
}
