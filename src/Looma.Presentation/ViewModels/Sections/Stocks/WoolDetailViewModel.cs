// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Refresh;
using Looma.Domain.Services;
using Looma.Presentation.Notifications;
using Looma.Presentation.Navigation;
using Looma.Presentation.UserControls;
using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.ViewModels.Sections.Stocks;

public partial class WoolDetailViewModel : PageViewModelBase
{
    private readonly INavigationService _nav;
    private readonly IWoolService _woolService;
    private readonly INotificationService _notifications;
    private readonly WoolStockCalculator _calculator;
    private readonly IDataRefreshService _refreshService;
    private Wool? _wool;

    [ObservableProperty]
    public partial int WoolId { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Brand { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Material { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Color { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double Weight { get; set; }

    [ObservableProperty]
    public partial double Length { get; set; }

    [ObservableProperty]
    public partial double StockWeight { get; set; }

    [ObservableProperty]
    public partial double StockLength { get; set; }

    [ObservableProperty]
    public partial double BatchQuantity { get; set; }

    [ObservableProperty]
    public partial double NeedleMinSize { get; set; }

    [ObservableProperty]
    public partial double NeedleMaxSize { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial List<string> Images { get; set; } = [];

    [ObservableProperty]
    public partial double? AdjustQuantity { get; set; }

    [ObservableProperty]
    public partial StockAdjustmentMode AdjustmentMode { get; set; } = StockAdjustmentMode.ByBall;

    public bool CanAdjust => AdjustQuantity > 0;
    partial void OnAdjustQuantityChanged(double? value) => OnPropertyChanged(nameof(CanAdjust));

    [RelayCommand]
    private async Task AdjustStockAsync(bool isAddition)
    {
        if (!CanAdjust || AdjustQuantity is null) return;

        var factor = AdjustmentMode switch
        {
            StockAdjustmentMode.ByBall => 0,
            StockAdjustmentMode.ByWeight => Weight,
            StockAdjustmentMode.ByLength => Length,
            _ => 0
        };

        var toSend = _calculator.ComputeStockQuantity(AdjustmentMode, isAddition, (double)AdjustQuantity, factor);
        
        var result = await _woolService.AddStockAsync(WoolId, toSend);
        if (result.Failed)
        {
            ErrorMessage = "Impossible de mettre à jour les données";
            _notifications.Error(result.Error ?? "Une erreur est survenue");
            return;
        }

        _notifications.Success("Stock correctement mis à jour");
    }

    public IList<StatItem> DetailStats =>
    [
        new() { Label = "Pelotes estimées", Value = BatchQuantity.ToString("N1"), Unit = "x", IsFirst = true },
        new() { Label = "Poids estimé", Value = StockWeight.ToString("N0"), Unit = "g" },
        new() { Label = "Longueur estimée", Value = StockLength.ToString("N0"), Unit = "m" }
    ];

    public IList<InfoItem> DetailInfos =>
    [
        new() { Label = "Marque", Value = Brand },
        new() { Label = "Matière", Value = Material },
        new() { Label = "Couleur", Value = Color, ColorHex = Color },
        new() { Label = "Aiguilles", Value = NeedleSizeDisplay },
        new() { Label = "Poids", Value = $"{Weight:N0}g" },
        new() { Label = "Longueur", Value = $"{Length:N0}m" },
    ];

    public string NeedleSizeDisplay =>
        $"{NeedleMinSize:G} – {NeedleMaxSize:G} mm";

    public WoolDetailViewModel(
        INavigationService nav,
        IWoolService woolService,
        INotificationService notifications,
        WoolStockCalculator calculator,
        IDataRefreshService refreshService)
    {
        _nav = nav;
        _woolService = woolService;
        _notifications = notifications;
        _calculator = calculator;
        _refreshService = refreshService;
        Title = "Détail laine";
    }

    public void Load(Wool wool)
    {
        _wool = wool;
        WoolId = wool.Id;
        Refresh(wool);
    }

    public override async void OnNavigatedTo()
    {
        RegisterRefresh(_refreshService, RefreshScope.Wools, RefreshAsync);
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (WoolId == 0) return;
        var wool = await _woolService.GetByIdAsync(WoolId);
        if (wool.Failed || wool.Value is null)
        {
            ErrorMessage = wool.Error ?? $"La laine {WoolId} est introuvable.";
            return;
        }

        Refresh(wool.Value);
    }

    private void Refresh(Wool wool)
    {
        ErrorMessage = null;
        Name = wool.Name;
        Brand = wool.Brand;
        Material = wool.Material;
        Color = wool.Color;

        Weight = wool.Weight;
        Length = wool.Length;
        StockWeight = wool.StockWeight;
        StockLength = wool.StockLength;
        BatchQuantity = wool.BatchQuantity;

        NeedleMinSize = wool.NeedleMinSize;
        NeedleMaxSize = wool.NeedleMaxSize;
        
        Images = wool.Types
            .Select(t => t.ToString().ToLower())
            .Select(s => $"avares://Looma.App/Assets/WoolTypeImages/{s}.png")
            .ToList();
        
        OnPropertyChanged(nameof(NeedleSizeDisplay));
        OnPropertyChanged(nameof(DetailStats));
        OnPropertyChanged(nameof(DetailInfos));
    }

    [RelayCommand]
    private void Edit() => _nav.NavigateTo<WoolFormViewModel>(vm => vm.InitEdit(_wool));

    [RelayCommand]
    private async Task ConfirmDeleteAsync()
    {
        IsBusy = true;
        try
        {
            var result = await _woolService.DeleteAsync(WoolId);
            if (result.Failed)
            {
                ErrorMessage = result.Error;
                _notifications.Error(result.Error ?? "Impossible de supprimer la laine.");
                return;
            }

            _notifications.Success("La laine a été supprimée.");
            _nav.GoBack();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void GoBack() => _nav.GoBack();
}
