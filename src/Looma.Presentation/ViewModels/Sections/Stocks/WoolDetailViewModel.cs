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
    private readonly IWoolRepository _repo;

    [ObservableProperty] private int _woolId;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _brand = string.Empty;
    [ObservableProperty] private string _material = string.Empty;
    [ObservableProperty] private string _color = string.Empty;
    [ObservableProperty] private double _lengthToWeightRatio;
    [ObservableProperty] private bool _showDeleteConfirm;

    public WoolDetailViewModel(INavigationService nav, IWoolRepository repo)
    {
        _nav = nav;
        _repo = repo;
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
        var wool = await _repo.GetByIdAsync(WoolId);
        if (wool is not null) Refresh(wool);
    }

    private void Refresh(Wool wool)
    {
        Name = wool.Name;
        Brand = wool.Brand;
        Material = wool.Material;
        Color = wool.Color;
        LengthToWeightRatio = wool.LengthToWeightRatio;
        ShowDeleteConfirm = false;
    }

    [RelayCommand]
    private void Edit() =>
        _nav.NavigateTo<WoolFormViewModel>(vm => vm.InitEdit(WoolId, Name, Brand, Material, Color, LengthToWeightRatio));

    [RelayCommand]
    private void AskDelete() => ShowDeleteConfirm = true;

    [RelayCommand]
    private void CancelDelete() => ShowDeleteConfirm = false;

    [RelayCommand]
    private async Task ConfirmDeleteAsync()
    {
        IsBusy = true;
        await _repo.DeleteAsync(WoolId);
        IsBusy = false;
        _nav.GoBack();
    }

    [RelayCommand]
    private void GoBack() => _nav.GoBack();
}