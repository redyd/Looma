// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using CommunityToolkit.Mvvm.ComponentModel;
using Looma.Presentation.Navigation;
using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.ViewModels.Sections.Projects;

public partial class ProjectsListViewModel : PageViewModelBase
{
    private readonly INavigationService _nav;

    [ObservableProperty] private int _itemId;

    public ProjectsListViewModel(INavigationService nav)
    {
        _nav = nav;
        Title = "Projets";
    }
}