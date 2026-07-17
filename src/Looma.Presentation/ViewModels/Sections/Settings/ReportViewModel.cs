// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Core;
using Looma.Domain.IServices;
using Looma.Presentation.Navigation;
using Looma.Presentation.Notifications;
using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.ViewModels.Sections.Settings;

public partial class ReportViewModel(
    INavigationService nav,
    IReportService reportService,
    INotificationService notifications)
    : PageViewModelBase
{
    [ObservableProperty]
    public partial bool IsSuggestion { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    public partial string Content { get; set; } = string.Empty;

    public override void OnNavigatedTo()
    {
        Title = Translation["SettingsReport_Title"];
    }

    private bool CanSubmit() => !string.IsNullOrWhiteSpace(Content);

    [RelayCommand]
    private void SelectBug() => IsSuggestion = false;

    [RelayCommand]
    private void SelectSuggestion() => IsSuggestion = true;

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync()
    {
        try
        {
            IsBusy = true;
            var type = IsSuggestion ? ReportType.Suggestion : ReportType.Bug;
            var result = await reportService.SubmitAsync(type, Content);

            if (result.Failed)
            {
                notifications.Error(Translation.Format("SettingsReport_Notifications_UnableToSend", result.Error ?? string.Empty));
                return;
            }

            notifications.Success(Translation["SettingsReport_Notifications_Sent"]);
            IsSuggestion = false;
            Content = string.Empty;
            nav.GoBack();
        }
        catch (Exception ex)
        {
            notifications.Error(Translation.Format("SettingsReport_Notifications_UnableToSend", ex.Message));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel() => nav.GoBack();
}
