using CommunityToolkit.Mvvm.ComponentModel;
using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.ViewModels.Main;

public partial class MainViewModel : ViewModelBase
{
    public SectionNavigationViewModel ProjectsSection { get; }
    public SectionNavigationViewModel StocksSection { get; }
    public SectionNavigationViewModel PatternsSection { get; }
    public SectionNavigationViewModel DocumentsSection { get; }

    [ObservableProperty] private int _selectedTabIndex;

    public MainViewModel(
        SectionNavigationViewModel projectsSection,
        SectionNavigationViewModel stocksSection,
        SectionNavigationViewModel patternsSection,
        SectionNavigationViewModel documentsSection)
    {
        PatternsSection = patternsSection;
        StocksSection = stocksSection;
        ProjectsSection = projectsSection;
        DocumentsSection = documentsSection;
        
        _selectedTabIndex = 0;
    }
}