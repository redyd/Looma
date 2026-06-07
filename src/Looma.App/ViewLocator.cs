// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Looma.Presentation.ViewModels.Base;
using System;
using System.Linq;

namespace Looma.App;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? data)
    {
        if (data is null) return null;

        var name = data.GetType().FullName!
            .Replace(".Presentation.ViewModels.", ".Views.Views.")
            .Replace("ViewModel", "View");

        var type = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(name))
            .FirstOrDefault(t => t is not null);

        if (type is not null)
            return (Control)Activator.CreateInstance(type)!;

        Console.WriteLine($"[ViewLocator] Introuvable : {name}");
        return new TextBlock { Text = $"Vue introuvable : {name}" };
    }

    public bool Match(object? data) => data is PageViewModelBase;
}