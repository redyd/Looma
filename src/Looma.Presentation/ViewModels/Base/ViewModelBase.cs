// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Threading;
using Looma.Presentation.Services;

namespace Looma.Presentation.ViewModels.Base;

public abstract class ViewModelBase : ObservableObject
{
    public TranslationService Translation => TranslationService.Current;

    protected static void RunOnUiThread(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
            return;
        }

        Dispatcher.UIThread.Post(action);
    }
}
