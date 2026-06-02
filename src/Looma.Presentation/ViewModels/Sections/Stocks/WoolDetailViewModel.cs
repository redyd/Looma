using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Entities;
using Looma.Domain.Repositories;
using Looma.Presentation.Navigation;
using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.ViewModels.Sections.Stocks;

public partial class WoolDetailViewModel : PageViewModelBase
{
    private readonly INavigationService _nav;
    private readonly IWoolRepository _woolRepo;
    private readonly IStockRepository _stockRepo;

    [ObservableProperty] private int _woolId;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _brand = string.Empty;
    [ObservableProperty] private string _material = string.Empty;
    [ObservableProperty] private string _color = string.Empty;
    [ObservableProperty] private double _lengthToWeightRatio;
    [ObservableProperty] private bool _showDeleteConfirm;
    [ObservableProperty] private double _totalWeightGrams;
    [ObservableProperty] private ObservableCollection<StockRowViewModel> _stockRows = [];
    [ObservableProperty] private double _needleMinSize;
    [ObservableProperty] private double _needleMaxSize;
    [ObservableProperty] private string? _errorMessage;

    public string NeedleSizeDisplay =>
        $"{NeedleMinSize:G} – {NeedleMaxSize:G} mm";

    public double TotalLengthMeters =>
        TotalWeightGrams / 100.0 * LengthToWeightRatio;

    public WoolDetailViewModel(
        INavigationService nav,
        IWoolRepository woolRepo,
        IStockRepository stockRepo)
    {
        _nav = nav;
        _woolRepo = woolRepo;
        _stockRepo = stockRepo;
        Title = "Détail laine";
    }

    public void Load(Wool wool)
    {
        WoolId = wool.Id;
        Refresh(wool);
    }

    public override async void OnNavigatedTo()
    {
        if (WoolId == 0) return;
        var wool = await _woolRepo.GetByIdAsync(WoolId);
        if (wool.Failed || wool.Value is null)
        {
            ErrorMessage = wool.Error ?? $"La laine {WoolId} est introuvable.";
            return;
        }

        Refresh(wool.Value);
        await LoadStocksAsync();
    }

    private void Refresh(Wool wool)
    {
        ErrorMessage = null;
        Name = wool.Name;
        Brand = wool.Brand;
        Material = wool.Material;
        Color = wool.Color;
        LengthToWeightRatio = wool.LengthToWeightRatio;
        NeedleMinSize = wool.NeedleMinSize;
        NeedleMaxSize = wool.NeedleMaxSize;
        ShowDeleteConfirm = false;
        OnPropertyChanged(nameof(NeedleSizeDisplay));
    }

    private async Task LoadStocksAsync()
    {
        var stocksResult = await _stockRepo.GetByWoolIdAsync(WoolId);
        if (stocksResult.Failed || stocksResult.Value is null)
        {
            ErrorMessage = stocksResult.Error ?? $"Impossible de charger les stocks de la laine {WoolId}.";
            StockRows = [];
            TotalWeightGrams = 0;
            OnPropertyChanged(nameof(TotalLengthMeters));
            return;
        }

        var stocks = stocksResult.Value;
        var totalResult = await _stockRepo.GetTotalWeightByWoolIdAsync(WoolId);
        TotalWeightGrams = totalResult.Succeeded
            ? totalResult.Value
            : stocks.Sum(s => s.WeightGrams);
        OnPropertyChanged(nameof(TotalLengthMeters));
        ErrorMessage = null;

        StockRows = new ObservableCollection<StockRowViewModel>(
            stocks.Select(s => new StockRowViewModel(s, LengthToWeightRatio, OnSaveRow, OnDeleteRow))
        );
    }

    private async Task OnSaveRow(StockRowViewModel row)
    {
        var weight = row.ParsedWeight();

        if (row.IsNew)
        {
            var result = await _stockRepo.AddAsync(new CreateStockRequest(WoolId, weight));
            if (result.Failed)
            {
                ErrorMessage = result.Error;
                return;
            }
        }
        else
        {
            var result = await _stockRepo.UpdateAsync(new UpdateStockRequest(row.StockId, null, weight));
            if (result.Failed)
            {
                ErrorMessage = result.Error;
                return;
            }
        }

        await LoadStocksAsync();
    }

    private async Task OnDeleteRow(StockRowViewModel row)
    {
        if (!row.IsNew)
        {
            var result = await _stockRepo.DeleteAsync(row.StockId);
            if (result.Failed)
            {
                ErrorMessage = result.Error;
                return;
            }
        }

        await LoadStocksAsync();
    }

    [RelayCommand]
    private void AddStockRow()
    {
        var placeholder = new Stock
        {
            Id = 0,
            WoolId = WoolId,
            WeightGrams = 0
        };
        var row = new StockRowViewModel(placeholder, LengthToWeightRatio, OnSaveRow, OnDeleteRow, isNew: true);
        StockRows.Add(row);
    }

    [RelayCommand]
    private void Edit() =>
        _nav.NavigateTo<WoolFormViewModel>(vm =>
            vm.InitEdit(WoolId, Name, Brand, Material, Color,
                LengthToWeightRatio, NeedleMinSize, NeedleMaxSize));

    [RelayCommand]
    private void AskDelete() => ShowDeleteConfirm = true;

    [RelayCommand]
    private void CancelDelete() => ShowDeleteConfirm = false;

    [RelayCommand]
    private async Task ConfirmDeleteAsync()
    {
        IsBusy = true;
        try
        {
            var result = await _woolRepo.DeleteAsync(WoolId);
            if (result.Failed)
            {
                ErrorMessage = result.Error;
                return;
            }

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
