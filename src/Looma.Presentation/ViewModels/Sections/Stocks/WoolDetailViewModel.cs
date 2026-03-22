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
        if (wool is not null) Refresh(wool);
        await LoadStocksAsync();
    }

    private void Refresh(Wool wool)
    {
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
        var stocks = await _stockRepo.GetByWoolIdAsync(WoolId);
        TotalWeightGrams = stocks.Sum(s => s.WeightGrams);
        OnPropertyChanged(nameof(TotalLengthMeters));

        StockRows = new ObservableCollection<StockRowViewModel>(
            stocks.Select(s => new StockRowViewModel(s, LengthToWeightRatio, OnSaveRow, OnDeleteRow))
        );
    }

    private async Task OnSaveRow(StockRowViewModel row)
    {
        var weight = row.ParsedWeight();

        if (row.IsNew)
        {
            var stock = Stock.Create(WoolId, weight);
            await _stockRepo.AddAsync(stock);
        }
        else
        {
            var stock = await _stockRepo.GetByWoolIdAsync(WoolId)
                .ContinueWith(t => t.Result.FirstOrDefault(s => s.Id == row.StockId));
            if (stock is null) return;
            stock.UpdateWeight(weight);
            await _stockRepo.UpdateAsync(stock);
        }

        await LoadStocksAsync();
    }

    private async Task OnDeleteRow(StockRowViewModel row)
    {
        if (!row.IsNew)
            await _stockRepo.DeleteAsync(row.StockId);
        await LoadStocksAsync();
    }

    [RelayCommand]
    private void AddStockRow()
    {
        var placeholder = Stock.Reconstitute(0, WoolId, 1);
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
        await _woolRepo.DeleteAsync(WoolId);
        IsBusy = false;
        _nav.GoBack();
    }

    [RelayCommand]
    private void GoBack() => _nav.GoBack();
}