// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Entities;
using Looma.Domain.Repositories;
using Looma.Domain.Search;
using Looma.Presentation.Notifications;
using Looma.Presentation.Navigation;
using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.ViewModels.Sections.Stocks;

public record WoolSummary(Wool Wool, ICommand OpenDetailCommand);

public partial class WoolListViewModel(
    INavigationService nav,
    IWoolRepository woolRepo,
    INotificationService notifications,
    WoolSearchSpec searchSpec)
    : PaginatePageViewModelBase<Wool, WoolSummary, int>(searchSpec)
{
    public override async void OnNavigatedTo() => await LoadAsync();

    private async Task LoadAsync()
    {
        GetEntityKey = wool => wool.Id;
        GetSummaryKey = summary => summary.Wool.Id;
        
        IsBusy = true;
        try
        {
            var woolsResult = await woolRepo.GetAllAsync();
            if (woolsResult.Failed || woolsResult.Value is null)
            {
                notifications.Error(woolsResult.Error ?? "Impossible de charger les laines.");
                ClearPagesState();
                return;
            }

            var all = woolsResult.Value;
            var summaries = all.Select(w =>
                new WoolSummary(w, new RelayCommand(() => nav.NavigateTo<WoolDetailViewModel>(vm => vm.Load(w)))))
                .ToList();
            
            ReloadPagesData(all, summaries);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void OpenAddForm() =>
        nav.NavigateTo<WoolFormViewModel>(vm => vm.InitCreate());
}