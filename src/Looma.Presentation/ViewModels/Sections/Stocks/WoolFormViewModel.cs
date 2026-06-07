// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Entities;
using Looma.Domain.Repositories;
using Looma.Domain.Request;
using Looma.Presentation.Notifications;
using Looma.Presentation.Navigation;
using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.ViewModels.Sections.Stocks;

public partial class WoolFormViewModel(INavigationService nav, IWoolRepository repo, INotificationService notifications)
    : PageViewModelBase
{
    private bool _isEdit;
    private int _editingId;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name = string.Empty;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _brand = string.Empty;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _material = string.Empty;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private Color _selectedColor = Colors.Gray;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _weight = string.Empty;
    
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _length = string.Empty;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _needleMinText = string.Empty;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _needleMaxText = string.Empty;

    [ObservableProperty] private string? _errorMessage;

    public string SelectedColorHex =>
        $"#{SelectedColor.R:X2}{SelectedColor.G:X2}{SelectedColor.B:X2}";

    public void InitCreate()
    {
        _isEdit = false;
        Title = "Nouvelle laine";
        Name = Brand = Material = Weight = Length = NeedleMinText = NeedleMaxText = string.Empty;
        SelectedColor = Colors.Gray;
        ErrorMessage = null;
    }

    public void InitEdit(Wool? wool)
    {
        if (wool is null)
        {
            ErrorMessage = "Aucune laine sélectionnée";
            return;
        }
        
        _isEdit = true;
        _editingId = wool.Id;
        Title = "Modifier la laine";
        Name = wool.Name;
        Brand = wool.Brand;
        Material = wool.Material;
        Weight = wool.Weight.ToString("G");
        Length = wool.Length.ToString("G");
        NeedleMinText = wool.NeedleMinSize.ToString("G");
        NeedleMaxText = wool.NeedleMaxSize.ToString("G");
        ErrorMessage = null;
        
        try
        {
            SelectedColor = Color.Parse(wool.Color);
        }
        catch
        {
            SelectedColor = Colors.Gray;
        }
    }

    partial void OnSelectedColorChanged(Color value) => OnPropertyChanged(nameof(SelectedColorHex));

    private bool CanSave() =>
        !string.IsNullOrWhiteSpace(Name) &&
        !string.IsNullOrWhiteSpace(Brand) &&
        !string.IsNullOrWhiteSpace(Material) &&
        double.TryParse(Weight, out var w) && w > 0 &&
        double.TryParse(Length, out var l) && l > 0 &&
        double.TryParse(NeedleMinText, out var nmin) && nmin > 0 &&
        double.TryParse(NeedleMaxText, out var nmax) && nmax >= nmin;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        ErrorMessage = null;

        if (!double.TryParse(Length, out var length) || length <= 0)
        {
            ErrorMessage = "La longeur doit être un nombre positif.";
            return;
        }
        
        if (!double.TryParse(Weight, out var weight) || weight <= 0)
        {
            ErrorMessage = "Le poids doit être un nombre positif.";
            return;
        }

        if (!double.TryParse(NeedleMinText, out var needleMin) || needleMin <= 0)
        {
            ErrorMessage = "La taille min d'aiguille doit être un nombre positif.";
            return;
        }

        if (!double.TryParse(NeedleMaxText, out var needleMax) || needleMax < needleMin)
        {
            ErrorMessage = "La taille max doit être supérieure ou égale à la taille min.";
            return;
        }

        try
        {
            IsBusy = true;
            if (_isEdit)
            {
                var result = await repo.UpdateAsync(new UpdateWoolRequest(
                    _editingId,
                    Name,
                    Brand,
                    Material,
                    SelectedColorHex,
                    weight,
                    length,
                    needleMin,
                    needleMax));
                if (result.Failed)
                {
                    ErrorMessage = result.Error;
                    notifications.Error(result.Error ?? "Impossible de sauvegarder la laine.");
                    return;
                }

                notifications.Success("La laine a été mise à jour.");
            }
            else
            {
                var result = await repo.AddAsync(new CreateWoolRequest(
                    Name,
                    Brand,
                    Material,
                    SelectedColorHex,
                    weight,
                    length,
                    1000,
                    needleMin,
                    needleMax));
                if (result.Failed)
                {
                    ErrorMessage = result.Error;
                    notifications.Error(result.Error ?? "Impossible de créer la laine.");
                    return;
                }

                notifications.Success("La laine a été créée.");
            }

            nav.GoBack();
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
    private void Cancel() => nav.GoBack();
}