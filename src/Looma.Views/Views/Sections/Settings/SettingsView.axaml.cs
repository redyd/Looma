// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Looma.Presentation.ViewModels.Sections.Settings;

namespace Looma.Views.Views.Sections.Settings;

public partial class SettingsView : UserControl
{
    private readonly DispatcherTimer _spinTimer;
    private INotifyPropertyChanged? _viewModel;
    private double _angle;

    public SettingsView()
    {
        InitializeComponent();

        UpdateSearchIcon.RenderTransform = new RotateTransform(0);
        _spinTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _spinTimer.Tick += (_, _) =>
        {
            _angle = (_angle + 7) % 360;
            if (UpdateSearchIcon.RenderTransform is RotateTransform transform)
            {
                transform.Angle = _angle;
            }
        };

        DataContextChanged += (_, _) => AttachViewModel();
        AttachViewModel();
    }

    protected override void OnPropertyChanged(Avalonia.AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property.Name == nameof(DataContext))
        {
            AttachViewModel();
        }
    }

    private void AttachViewModel()
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = DataContext as INotifyPropertyChanged;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        UpdateSpinState();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.IsCheckingForUpdates))
        {
            UpdateSpinState();
        }
    }

    private void UpdateSpinState()
    {
        if (DataContext is SettingsViewModel { IsCheckingForUpdates: true })
        {
            if (!_spinTimer.IsEnabled)
                _spinTimer.Start();
            return;
        }

        _spinTimer.Stop();
        _angle = 0;
        if (UpdateSearchIcon.RenderTransform is RotateTransform transform)
        {
            transform.Angle = 0;
        }
    }
}
