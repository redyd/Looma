// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Entities;
using Looma.Domain.Search;
using Looma.Domain.Services;
using Looma.Presentation.Navigation;
using Looma.Presentation.Notifications;
using Looma.Presentation.ViewModels.Base;
using Looma.Presentation.ViewModels.Shared.Patterns;

namespace Looma.Presentation.ViewModels.Sections.Patterns;

public partial class PatternsListViewModel(
    INavigationService nav,
    IPatternService patternService,
    INotificationService notifications) : PaginatePageViewModelBase<Pattern, PatternSummaryViewModel, int>(new PatternSearchSpec())
{
    public override async void OnNavigatedTo() => await LoadAsync();

    private async Task LoadAsync()
    {
        GetEntityKey = pattern => pattern.Id;
        GetSummaryKey = summary => summary.Pattern.Id;
        
        Title = "Mes patrons";
        IsBusy = true;
        try
        {
            var result = await patternService.GetAllAsync();
            if (result.Failed || result.Value is null)
            {
                notifications.Error(result.Error ?? "Impossible de charger les patrons.");
                ClearPagesState();
                return;
            }

            var all = result.Value;
            var summaries = all.Select(BuildSummary).ToList();

            ReloadPagesData(all, summaries);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private PatternSummaryViewModel BuildSummary(Pattern pattern) =>
        new(
            pattern,
            pattern.Documents.Count,
            pattern.Projects.Count,
            !string.IsNullOrWhiteSpace(pattern.Url),
            new RelayCommand(() => nav.NavigateTo<PatternsDetailViewModel>(vm => vm.Load(pattern))));

    [RelayCommand]
    private void OpenAddForm() =>
        nav.NavigateTo<PatternsFormViewModel>(vm => vm.InitCreate());
}
