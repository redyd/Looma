// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

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
    public SectionNavigationViewModel SettingsSection { get; }
    public INotificationService Notifications { get; }

    [ObservableProperty] private int _selectedTabIndex;

    public MainViewModel(
        SectionNavigationViewModel projectsSection,
        SectionNavigationViewModel stocksSection,
        SectionNavigationViewModel patternsSection,
        SectionNavigationViewModel documentsSection,
        SectionNavigationViewModel settingsSection,
        INotificationService notifications)
    {
        PatternsSection = patternsSection;
        StocksSection = stocksSection;
        ProjectsSection = projectsSection;
        DocumentsSection = documentsSection;
        SettingsSection = settingsSection;
        Notifications = notifications;

        _selectedTabIndex = 0;
    }
}
