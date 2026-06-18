// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace Looma.Views.UserControls;

public partial class PaginationControl : UserControl
{
    public static readonly StyledProperty<ICommand?> PreviousPageCommandProperty =
        AvaloniaProperty.Register<PaginationControl, ICommand?>(nameof(PreviousPageCommand));

    public static readonly StyledProperty<ICommand?> NextPageCommandProperty =
        AvaloniaProperty.Register<PaginationControl, ICommand?>(nameof(NextPageCommand));

    public static readonly StyledProperty<bool> HasPreviousPageProperty =
        AvaloniaProperty.Register<PaginationControl, bool>(nameof(HasPreviousPage));

    public static readonly StyledProperty<bool> HasNextPageProperty =
        AvaloniaProperty.Register<PaginationControl, bool>(nameof(HasNextPage));

    public static readonly StyledProperty<string> PageInfoProperty =
        AvaloniaProperty.Register<PaginationControl, string>(nameof(PageInfo), defaultValue: string.Empty);

    public ICommand? PreviousPageCommand
    {
        get => GetValue(PreviousPageCommandProperty);
        set => SetValue(PreviousPageCommandProperty, value);
    }

    public ICommand? NextPageCommand
    {
        get => GetValue(NextPageCommandProperty);
        set => SetValue(NextPageCommandProperty, value);
    }

    public bool HasPreviousPage
    {
        get => GetValue(HasPreviousPageProperty);
        set => SetValue(HasPreviousPageProperty, value);
    }

    public bool HasNextPage
    {
        get => GetValue(HasNextPageProperty);
        set => SetValue(HasNextPageProperty, value);
    }

    public string PageInfo
    {
        get => GetValue(PageInfoProperty);
        set => SetValue(PageInfoProperty, value);
    }

    public PaginationControl()
    {
        InitializeComponent();
    }
}
