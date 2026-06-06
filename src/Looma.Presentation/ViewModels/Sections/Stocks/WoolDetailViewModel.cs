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
    private readonly INotificationService _notifications;
    private Wool? _wool;

    [ObservableProperty] private int _woolId;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _brand = string.Empty;
    [ObservableProperty] private string _material = string.Empty;
    [ObservableProperty] private string _color = string.Empty;

    [ObservableProperty] private double _weight;
    [ObservableProperty] private double _length;
    [ObservableProperty] private double _stockWeight;
    [ObservableProperty] private double _stockLength;
    [ObservableProperty] private double _batchQuantity;

    [ObservableProperty] private double _needleMinSize;
    [ObservableProperty] private double _needleMaxSize;
    [ObservableProperty] private string? _errorMessage;

    public IList<StatItem> DetailStats =>
    [
        new() { Label = "Pelotes estimées", Value = BatchQuantity.ToString("N0"), Unit = "x", IsFirst = true },
        new() { Label = "Poids estimé", Value = StockWeight.ToString("N0"), Unit = "g" },
        new() { Label = "Longueur estimée", Value = StockLength.ToString("N0"), Unit = "m" }
    ];

    public IList<InfoItem> DetailInfos =>
    [
        new() { Label = "Marque", Value = Brand },
        new() { Label = "Matière", Value = Material },
        new() { Label = "Couleur", Value = Color, ColorHex = Color },
        new() { Label = "Aiguilles", Value = NeedleSizeDisplay }
    ];

    public string NeedleSizeDisplay =>
        $"{NeedleMinSize:G} – {NeedleMaxSize:G} mm";

    public WoolDetailViewModel(
        INavigationService nav,
        IWoolRepository woolRepo,
        INotificationService notifications)
    {
        _nav = nav;
        _woolRepo = woolRepo;
        _notifications = notifications;
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
        if (WoolId == 0) return;
        var wool = await _woolRepo.GetByIdAsync(WoolId);
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