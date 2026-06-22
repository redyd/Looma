// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Looma.Presentation.ViewModels.Base;
using Looma.Presentation.ViewModels.Main;
using System.Windows.Input;

namespace Looma.Views.Views.Main;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainViewModel mainViewModel)
            return;

        var source = e.Source as Control;

        if (TryHandleOverlayShortcut(mainViewModel, e.Key, source))
        {
            e.Handled = true;
            return;
        }

        var activeSection = GetActiveSection(mainViewModel);
        var activePage = activeSection?.CurrentPage;
        if (activePage is null)
            return;

        var command = e.Key switch
        {
            Key.Enter when CanHandleSubmit(source) => GetSubmitCommand(activePage),
            Key.Escape when CanHandleCancel(source) => GetCancelCommand(activePage) ?? activeSection?.GoBackCommand,
            Key.Left or Key.Up when CanHandlePagination(source) => GetCommand(activePage, "PreviousPageCommand"),
            Key.Right or Key.Down when CanHandlePagination(source) => GetCommand(activePage, "NextPageCommand"),
            _ => null
        };

        if (Execute(command))
            e.Handled = true;
    }

    private static bool TryHandleOverlayShortcut(MainViewModel viewModel, Key key, Control? source)
    {
        if (viewModel.IsReleaseNotesVisible)
            return key == Key.Escape && Execute(viewModel.CloseReleaseNotesCommand);

        if (viewModel.IsUpdatePromptVisible)
        {
            if (key == Key.Escape)
                return Execute(viewModel.CloseUpdatePromptCommand);

            if (key == Key.Enter && CanHandleSubmit(source))
                return Execute(viewModel.ConfirmUpdateCommand);
        }

        return false;
    }

    private static SectionNavigationViewModel? GetActiveSection(MainViewModel viewModel) =>
        viewModel.SelectedTabIndex switch
        {
            0 => viewModel.ProjectsSection,
            1 => viewModel.StocksSection,
            2 => viewModel.PatternsSection,
            3 => viewModel.DocumentsSection,
            4 => viewModel.StatisticsSection,
            5 => viewModel.SettingsSection,
            _ => null
        };

    private static ICommand? GetSubmitCommand(PageViewModelBase page) =>
        GetCommand(page, "SaveCommand") ??
        GetCommand(page, "ConfirmCommand") ??
        GetCommand(page, "SaveNoteCommand");

    private static ICommand? GetCancelCommand(PageViewModelBase page) =>
        GetCommand(page, "CancelCommand") ??
        GetCommand(page, "GoBackCommand");

    private static ICommand? GetCommand(object source, string propertyName) =>
        source.GetType().GetProperty(propertyName)?.GetValue(source) as ICommand;

    private static bool Execute(ICommand? command)
    {
        if (command is null || !command.CanExecute(null))
            return false;

        command.Execute(null);
        return true;
    }

    private static bool CanHandleSubmit(Control? source)
    {
        if (source is TextBox { AcceptsReturn: true })
            return false;

        return source is null || !IsInsideSelectionOrButtonControl(source);
    }

    private static bool CanHandleCancel(Control? source) =>
        source is null || !IsInsideSelectionControl(source);

    private static bool CanHandlePagination(Control? source) =>
        source is null || !IsInsideEditingOrSelectionControl(source);

    private static bool IsInsideEditingOrSelectionControl(Control source) =>
        IsOrIsInside<TextBox>(source) ||
        IsInsideSelectionControl(source);

    private static bool IsInsideSelectionOrButtonControl(Control source) =>
        IsOrIsInside<Button>(source) ||
        IsInsideSelectionControl(source);

    private static bool IsInsideSelectionControl(Control source) =>
        IsOrIsInside<ComboBox>(source) ||
        IsOrIsInside<DatePicker>(source) ||
        IsOrIsInside<NumericUpDown>(source) ||
        IsInsideControlNamed(source, "ColorPicker");

    private static bool IsOrIsInside<TControl>(Control source)
        where TControl : Control =>
        source is TControl || source.GetVisualAncestors().OfType<TControl>().Any();

    private static bool IsInsideControlNamed(Control source, string typeName) =>
        source.GetType().Name == typeName ||
        source.GetVisualAncestors().OfType<Control>().Any(control => control.GetType().Name == typeName);
}
