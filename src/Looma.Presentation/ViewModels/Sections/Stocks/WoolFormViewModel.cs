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

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _brand = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _material = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _color = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _lengthToWeightRatioText = string.Empty;

    [ObservableProperty] private string? _errorMessage;

    public WoolFormViewModel(INavigationService nav, IWoolRepository repo)
    {
        _nav = nav;
        _repo = repo;
    }

    public void InitCreate()
    {
        _isEdit = false;
        Title = "Nouvelle laine";
        Name = Brand = Material = Color = LengthToWeightRatioText = string.Empty;
        ErrorMessage = null;
    }

    public void InitEdit(int id, string name, string brand, string material, string color, double ratio)
    {
        _isEdit = true;
        _editingId = id;
        Title = "Modifier la laine";
        Name = name;
        Brand = brand;
        Material = material;
        Color = color;
        LengthToWeightRatioText = ratio.ToString();
        ErrorMessage = null;
    }

    private bool CanSave() =>
        !string.IsNullOrWhiteSpace(Name) &&
        !string.IsNullOrWhiteSpace(Brand) &&
        !string.IsNullOrWhiteSpace(Material) &&
        !string.IsNullOrWhiteSpace(Color) &&
        double.TryParse(LengthToWeightRatioText, out var r) && r > 0;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        ErrorMessage = null;

        if (!double.TryParse(LengthToWeightRatioText, out var ratio) || ratio <= 0)
        {
            ErrorMessage = "Le ratio longueur/poids doit être un nombre positif.";
            return;
        }

        try
        {
            IsBusy = true;

            if (_isEdit)
            {
                var wool = await _repo.GetByIdAsync(_editingId);
                if (wool is null) { ErrorMessage = "Laine introuvable."; return; }
                wool.Update(Name, Brand, Material, Color, ratio);
                await _repo.UpdateAsync(wool);
            }
            else
            {
                var wool = Wool.Create(Name, Brand, Material, Color, ratio);
                await _repo.AddAsync(wool);
            }

            _nav.GoBack();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel() => _nav.GoBack();
}