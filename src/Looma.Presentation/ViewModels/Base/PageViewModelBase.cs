// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using CommunityToolkit.Mvvm.ComponentModel;

namespace Looma.Presentation.ViewModels.Base;

public abstract partial class PageViewModelBase : ViewModelBase
{
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _title = string.Empty;

    /// <summary>Appelé à chaque fois que la page devient active.</summary>
    public virtual void OnNavigatedTo()
    {
    }

    /// <summary>Appelé quand on quitte la page.</summary>
    public virtual void OnNavigatedFrom()
    {
    }
}