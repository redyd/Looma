// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Looma.Views.Views.Sections.Documents;

public partial class DocumentsListView : UserControl
{
    public DocumentsListView()
    {
        InitializeComponent();
    }

    private void OnDeleteClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Popup popup })
        {
            popup.IsOpen = true;
        }
    }

    private void OnCancelDeleteClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Popup popup })
        {
            popup.IsOpen = false;
        }
    }

    private void OnConfirmDeleteClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Popup popup })
        {
            popup.IsOpen = false;
        }
    }
}
