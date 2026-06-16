// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Extensions;
using Looma.Domain.Request;
using Looma.Domain.Services;
using Looma.Presentation.Navigation;
using Looma.Presentation.Notifications;
using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.ViewModels.Sections.Projects;

public partial class ProjectsFinishViewModel(
    INavigationService nav,
    IProjectService projectService,
    IWoolStockService stockService,
    INotificationService notifications)
    : PageViewModelBase
{
    private Project? _project;

    [ObservableProperty]
    public partial int ProjectId { get; set; }
    [ObservableProperty]
    public partial string ProjectName { get; set; } = string.Empty;
    [ObservableProperty]
    public partial DateTimeOffset? EndDate { get; set; } = DateTimeOffset.Now;
    [ObservableProperty]
    public partial StockAdjustmentMode DeductionMode { get; set; } = StockAdjustmentMode.ByBall;
    [ObservableProperty]
    public partial ObservableCollection<ProjectFinishWoolViewModel> Wools { get; set; } = [];
    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public bool HasWools => Wools.Count > 0;
    public IReadOnlyList<StockAdjustmentMode> DeductionModes { get; } = Enum.GetValues<StockAdjustmentMode>().ToList();
    public string TotalToDeductDisplay => $"{Wools.Sum(w => w.QuantityToDeduct):N2} {DeductionUnit}";
    public string DeductionUnit => DeductionMode switch
    {
        StockAdjustmentMode.ByWeight => "g",
        StockAdjustmentMode.ByLength => "m",
        _ => "pelote(s)"
    };

    public void Load(int projectId)
    {
        ProjectId = projectId;
    }

    public override async void OnNavigatedTo()
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (ProjectId == 0)
            return;

        var result = await projectService.GetByIdAsync(ProjectId);
        if (result.Failed || result.Value is null)
        {
            ErrorMessage = result.Error ?? $"Le projet {ProjectId} est introuvable.";
            notifications.Error(ErrorMessage);
            return;
        }

        ApplyProject(result.Value);
    }

    private void ApplyProject(Project project)
    {
        _project = project;
        ErrorMessage = null;
        ProjectName = project.Name;
        EndDate = DateTimeOffset.Now;
        Wools = new ObservableCollection<ProjectFinishWoolViewModel>(
            project.Wools.Select(usage => new ProjectFinishWoolViewModel(usage, DeductionMode, NotifyTotalChanged)));

        OnPropertyChanged(nameof(HasWools));
        OnPropertyChanged(nameof(TotalToDeductDisplay));
    }

    private void NotifyTotalChanged() =>
        OnPropertyChanged(nameof(TotalToDeductDisplay));

    partial void OnDeductionModeChanged(StockAdjustmentMode value)
    {
        foreach (var wool in Wools)
        {
            wool.SetDeductionMode(value);
        }

        OnPropertyChanged(nameof(DeductionUnit));
        OnPropertyChanged(nameof(TotalToDeductDisplay));
    }

    [RelayCommand]
    private async Task ConfirmAsync()
    {
        if (_project is null)
            return;

        if (EndDate is null)
        {
            notifications.Error("Indiquez une date de fin.");
            return;
        }

        foreach (var wool in Wools)
        {
            if (wool.QuantityToDeduct < 0)
            {
                notifications.Error("Les quantités à retirer doivent être positives.");
                return;
            }

            if (wool.StockToDeduct > wool.Usage.Wool.Stock)
            {
                notifications.Error($"Le stock disponible est insuffisant pour {wool.Name}.");
                return;
            }
        }

        IsBusy = true;
        try
        {
            foreach (var wool in Wools)
            {
                var targetStockUsed = wool.Usage.StockAlreadyUsed + wool.StockToDeduct;
                var delta = targetStockUsed - wool.Usage.StockUsed;
                if (Math.Abs(delta) < 0.001)
                    continue;

                var adjustResult = await stockService.AdjustWoolUsageAsync(new AdjustProjectWoolUsageRequest(
                    ProjectId,
                    wool.Usage.Wool.Id,
                    DeductionMode,
                    delta > 0,
                    wool.FormatStockQuantity(Math.Abs(delta)),
                    false));

                if (adjustResult.Failed)
                {
                    notifications.Error(adjustResult.Error ?? "Impossible de mettre à jour la laine utilisée.");
                    return;
                }
            }

            var updateResult = await projectService.UpdateAsync(new UpdateProjectRequest(
                ProjectId,
                _project.Name,
                Status.Finished,
                _project.Note,
                _project.BeginDate,
                EndDate.ToDateOnly(),
                _project.Pattern?.Id,
                _project.Wools.Select(w => w.Wool.Id).ToList()));

            if (updateResult.Failed)
            {
                notifications.Error(updateResult.Error ?? "Impossible de terminer le projet.");
                return;
            }

            notifications.Success("Le projet a été terminé.");
            nav.GoBack();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel() => nav.GoBack();
}

public partial class ProjectFinishWoolViewModel(
    WoolUsage usage,
    StockAdjustmentMode deductionMode,
    Action quantityChanged) : ObservableObject
{
    private StockAdjustmentMode _deductionMode = deductionMode;
    private double _quantityToDeduct = FormatStockQuantity(usage.PendingStockToDeduct, usage.Wool, deductionMode);

    public WoolUsage Usage { get; } = usage;
    public string Name => Usage.Wool.Name;
    public string Brand => Usage.Wool.Brand;
    public string Color => Usage.Wool.Color;
    public string AvailableDisplay => $"{Usage.Wool.Stock / 1000:N2} pelote(s)";
    public string AlreadyDeductedDisplay => $"{Usage.StockAlreadyUsed / 1000:N2} pelote(s)";

    public double QuantityToDeduct
    {
        get => _quantityToDeduct;
        set
        {
            if (!SetProperty(ref _quantityToDeduct, value))
                return;

            OnPropertyChanged(nameof(StockToDeduct));
            quantityChanged();
        }
    }

    public double StockToDeduct => ComputeStockQuantity(QuantityToDeduct);

    public void SetDeductionMode(StockAdjustmentMode value)
    {
        if (_deductionMode == value)
            return;

        var stockToDeduct = StockToDeduct;
        _deductionMode = value;
        QuantityToDeduct = FormatStockQuantity(stockToDeduct);
    }

    public double FormatStockQuantity(double stock) =>
        FormatStockQuantity(stock, Usage.Wool, _deductionMode);

    private double ComputeStockQuantity(double quantity) =>
        _deductionMode switch
        {
            StockAdjustmentMode.ByWeight => quantity / Usage.Wool.Weight * 1000,
            StockAdjustmentMode.ByLength => quantity / Usage.Wool.Length * 1000,
            _ => quantity * 1000
        };

    private static double FormatStockQuantity(double stock, Wool wool, StockAdjustmentMode mode) =>
        mode switch
        {
            StockAdjustmentMode.ByWeight => stock / 1000 * wool.Weight,
            StockAdjustmentMode.ByLength => stock / 1000 * wool.Length,
            _ => stock / 1000
        };
}
