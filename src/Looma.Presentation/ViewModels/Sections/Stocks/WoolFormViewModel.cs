using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Entities;
using Looma.Domain.Repositories;
using Looma.Presentation.Navigation;
using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.ViewModels.Sections.Stocks;

public partial class WoolFormViewModel : PageViewModelBase
{
    private readonly INavigationService _nav;
    private readonly IWoolRepository _repo;
    private bool _isEdit;
    private int _editingId;

    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name = string.Empty;

    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _brand = string.Empty;

    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _material = string.Empty;

    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private Color _selectedColor = Colors.Gray;

    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _lengthToWeightRatioText = string.Empty;

    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _needleMinText = string.Empty;

    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _needleMaxText = string.Empty;

    [ObservableProperty] private string? _errorMessage;

    public string SelectedColorHex =>
        $"#{SelectedColor.R:X2}{SelectedColor.G:X2}{SelectedColor.B:X2}";

    public WoolFormViewModel(INavigationService nav, IWoolRepository repo)
    {
        _nav = nav;
        _repo = repo;
    }

    public void InitCreate()
    {
        _isEdit = false;
        Title = "Nouvelle laine";
        Name = Brand = Material = LengthToWeightRatioText = NeedleMinText = NeedleMaxText = string.Empty;
        SelectedColor = Colors.Gray;
        ErrorMessage = null;
    }

    public void InitEdit(int id, string name, string brand, string material,
        string colorHex, double ratio, double needleMin, double needleMax)
    {
        _isEdit = true;
        _editingId = id;
        Title = "Modifier la laine";
        Name = name;
        Brand = brand;
        Material = material;
        LengthToWeightRatioText = ratio.ToString("G");
        NeedleMinText = needleMin.ToString("G");
        NeedleMaxText = needleMax.ToString("G");
        ErrorMessage = null;
        try { SelectedColor = Color.Parse(colorHex); }
        catch { SelectedColor = Colors.Gray; }
    }

    partial void OnSelectedColorChanged(Color value) =>
        OnPropertyChanged(nameof(SelectedColorHex));

    private bool CanSave() =>
        !string.IsNullOrWhiteSpace(Name) &&
        !string.IsNullOrWhiteSpace(Brand) &&
        !string.IsNullOrWhiteSpace(Material) &&
        double.TryParse(LengthToWeightRatioText, out var r) && r > 0 &&
        double.TryParse(NeedleMinText, out var nmin) && nmin > 0 &&
        double.TryParse(NeedleMaxText, out var nmax) && nmax >= nmin;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        ErrorMessage = null;

        if (!double.TryParse(LengthToWeightRatioText, out var ratio) || ratio <= 0)
        { ErrorMessage = "Le ratio longueur/poids doit être un nombre positif."; return; }

        if (!double.TryParse(NeedleMinText, out var needleMin) || needleMin <= 0)
        { ErrorMessage = "La taille min d'aiguille doit être un nombre positif."; return; }

        if (!double.TryParse(NeedleMaxText, out var needleMax) || needleMax < needleMin)
        { ErrorMessage = "La taille max doit être supérieure ou égale à la taille min."; return; }

        try
        {
            IsBusy = true;
            if (_isEdit)
            {
                var wool = await _repo.GetByIdAsync(_editingId);
                if (wool is null) { ErrorMessage = "Laine introuvable."; return; }
                wool.Update(Name, Brand, Material, SelectedColorHex, ratio, needleMin, needleMax);
                await _repo.UpdateAsync(wool);
            }
            else
            {
                var wool = Wool.Create(Name, Brand, Material, SelectedColorHex, ratio, needleMin, needleMax);
                await _repo.AddAsync(wool);
            }
            _nav.GoBack();
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void Cancel() => _nav.GoBack();
}