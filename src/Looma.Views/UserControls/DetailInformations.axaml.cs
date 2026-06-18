// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Looma.Presentation.UserControls;
using System.Diagnostics;

namespace Looma.Views.UserControls;

public partial class DetailInformations : UserControl
{
    public static readonly StyledProperty<IList<StatItem>> StatsProperty =
        AvaloniaProperty.Register<DetailInformations, IList<StatItem>>(
            nameof(Stats), defaultValue: []);

    public static readonly StyledProperty<IList<InfoItem>> InfosProperty =
        AvaloniaProperty.Register<DetailInformations, IList<InfoItem>>(
            nameof(Infos), defaultValue: []);

    public static readonly StyledProperty<string> InfoTitleProperty =
        AvaloniaProperty.Register<DetailInformations, string>(nameof(InfoTitle), defaultValue: "Informations");

    public static readonly StyledProperty<string?> InfoIconKindProperty =
        AvaloniaProperty.Register<DetailInformations, string?>(nameof(InfoIconKind));
    
    public static readonly StyledProperty<string?> ImageProperty =
        AvaloniaProperty.Register<DetailInformations, string?>(nameof(Image));

    public string? Image
    {
        get => GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }

    public IList<StatItem> Stats
    {
        get => GetValue(StatsProperty);
        set => SetValue(StatsProperty, value);
    }

    public IList<InfoItem> Infos
    {
        get => GetValue(InfosProperty);
        set => SetValue(InfosProperty, value);
    }

    public string InfoTitle
    {
        get => GetValue(InfoTitleProperty);
        set => SetValue(InfoTitleProperty, value);
    }

    public string? InfoIconKind
    {
        get => GetValue(InfoIconKindProperty);
        set => SetValue(InfoIconKindProperty, value);
    }

    public DetailInformations()
    {
        InitializeComponent();
    }

    private void OnInfoLinkClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { DataContext: InfoItem { IsLink: true } item }
            || string.IsNullOrWhiteSpace(item.Value))
            return;

        try
        {
            Process.Start(new ProcessStartInfo(item.Value)
            {
                UseShellExecute = true
            });
        }
        catch
        {
            // The detail view should not crash if the system cannot open the link.
        }
    }
}
