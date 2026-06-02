using CommunityToolkit.Mvvm.ComponentModel;
using Looma.Presentation.Notifications;
using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.ViewModels.Main;

public partial class MainViewModel : ViewModelBase
{
    public SectionNavigationViewModel ProjectsSection { get; }
    public SectionNavigationViewModel StocksSection { get; }
    public SectionNavigationViewModel PatternsSection { get; }
    public SectionNavigationViewModel DocumentsSection { get; }
    public INotificationService Notifications { get; }

    [ObservableProperty] private int _selectedTabIndex;

    public MainViewModel(
        SectionNavigationViewModel projectsSection,
        SectionNavigationViewModel stocksSection,
        SectionNavigationViewModel patternsSection,
        SectionNavigationViewModel documentsSection,
        INotificationService notifications)
    {
        PatternsSection = patternsSection;
        StocksSection = stocksSection;
        ProjectsSection = projectsSection;
        DocumentsSection = documentsSection;
        Notifications = notifications;

        _selectedTabIndex = 0;
    }
}
