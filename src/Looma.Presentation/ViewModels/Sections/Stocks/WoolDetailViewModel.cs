using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Entities;
using Looma.Domain.Repositories;
using Looma.Presentation.Notifications;
using Looma.Presentation.Navigation;
using Looma.Presentation.UserControls;
using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.ViewModels.Sections.Stocks;

public partial class WoolDetailViewModel : PageViewModelBase
{
    private readonly INavigationService _nav;
    private readonly IWoolRepository _woolRepo;
    private readonly IStockRepository _stockRepo;
    private readonly INotificationService _notifications;

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
    
    public IList<StatItem> DetailStats =>
    [
        new() { Label = "Pelottes estimée", Value = "-1", Unit = "x"},
        new() { Label = "Stock total", Value = TotalWeightGrams.ToString("N0"), Unit = "g" },
        new() { Label = "Longueur estimée", Value = TotalLengthMeters.ToString("N0"), Unit = "m" }
    ];

    public IList<InfoItem> DetailInfos =>
    [
        new() { Label = "Marque", Value = Brand },
        new() { Label = "Matière", Value = Material },
        new() { Label = "Couleur", Value = Color, ColorHex = Color },
        new() { Label = "Ratio", Value = $"{LengthToWeightRatio:N1} m/100g" },
        new() { Label = "Aiguilles", Value = NeedleSizeDisplay }
    ];

    public string NeedleSizeDisplay =>
        $"{NeedleMinSize:G} – {NeedleMaxSize:G} mm";

    public double TotalLengthMeters =>
        TotalWeightGrams / 100.0 * LengthToWeightRatio;

    public WoolDetailViewModel(
        INavigationService nav,
        IWoolRepository woolRepo,
        IStockRepository stockRepo,
        INotificationService notifications)
    {
        _nav = nav;
        _woolRepo = woolRepo;
        _stockRepo = stockRepo;
        _notifications = notifications;
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
        OnPropertyChanged(nameof(DetailStats));
        OnPropertyChanged(nameof(DetailInfos));
    }

    private async Task LoadStocksAsync()
    {
        var stocksResult = await _stockRepo.GetByWoolIdAsync(WoolId);
        if (stocksResult.Failed || stocksResult.Value is null)
        {
            ErrorMessage = stocksResult.Error ?? $"Impossible de charger les stocks de la laine {WoolId}.";
            _notifications.Error(ErrorMessage);
            StockRows = [];
            TotalWeightGrams = 0;
            OnPropertyChanged(nameof(TotalLengthMeters));
            OnPropertyChanged(nameof(DetailStats));
            return;
        }

        var stocks = stocksResult.Value;
        var totalResult = await _stockRepo.GetTotalWeightByWoolIdAsync(WoolId);
        TotalWeightGrams = totalResult.Succeeded
            ? totalResult.Value
            : stocks.Sum(s => s.WeightGrams);

        OnPropertyChanged(nameof(TotalLengthMeters));
        OnPropertyChanged(nameof(DetailStats));

        ErrorMessage = null;

        StockRows = new ObservableCollection<StockRowViewModel>(
            stocks.Select(s => new StockRowViewModel(s, LengthToWeightRatio, OnSaveRow, OnDeleteRow))
        );
    }

    private async Task OnSaveRow(StockRowViewModel row)
    {
        var weight = row.ParsedWeight();

        var result = await _stockRepo.AddAsync(new CreateStockRequest(WoolId, weight));
        if (result.Failed)
        {
            ErrorMessage = result.Error;
            _notifications.Error(result.Error ?? "Impossible d'ajouter le stock.");
            return;
        }

        _notifications.Success("Stock ajouté.");

        await LoadStocksAsync();
    }

    private async Task OnDeleteRow(StockRowViewModel row)
    {
        var result = await _stockRepo.DeleteAsync(row.StockId);
        if (result.Failed)
        {
            ErrorMessage = result.Error;
            _notifications.Error(result.Error ?? "Impossible de supprimer le stock.");
            return;
        }

        _notifications.Success("Stock supprimé.");

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