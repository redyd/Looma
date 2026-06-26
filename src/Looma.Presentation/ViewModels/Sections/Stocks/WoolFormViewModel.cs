// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Entities;
using Looma.Domain.Request;
using Looma.Domain.Services;
using Looma.Presentation.Notifications;
using Looma.Presentation.Navigation;
using Looma.Presentation.ViewModels.Base;
using Looma.Domain.IServices;

namespace Looma.Presentation.ViewModels.Sections.Stocks;

public partial class WoolFormViewModel(INavigationService nav, IWoolService woolService, INotificationService notifications)
    : PageViewModelBase
{
    private bool _isEdit;
    private int _editingId;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string Brand { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string Material { get; set; } = string.Empty;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string Weight { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string Length { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial WoolNeedleRangeSummary? SelectedNeedleRange { get; set; }

    public IReadOnlyList<WoolNeedleRangeSummary> NeedleRanges { get; } = [.. Wool.NeedleRanges.Select(n => new WoolNeedleRangeSummary(n))];

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }
    
    [ObservableProperty]
    public partial Color SelectedColor { get; set; } = Colors.Gray;

    [ObservableProperty]
    public partial ObservableCollection<string> AllColors { get; set; } = [];
    public bool HasColors => AllColors.Count > 0;

    public string SelectedColorHex =>
        $"#{SelectedColor.R:X2}{SelectedColor.G:X2}{SelectedColor.B:X2}";

    partial void OnSelectedColorChanged(Color value) => OnPropertyChanged(nameof(SelectedColorHex));
    
    partial void OnAllColorsChanged(ObservableCollection<string> value)
    {
        value.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasColors));
        OnPropertyChanged(nameof(HasColors));
    }
    
    [RelayCommand]
    private void AddColor()
    {
        var hex = SelectedColorHex;
        if (!AllColors.Contains(hex))
        {
            AllColors.Add(hex);
            OnPropertyChanged(nameof(HasColors));
        }
    }

    [RelayCommand]
    private void RemoveColor(string hex)
    {
        AllColors.Remove(hex);
        OnPropertyChanged(nameof(HasColors));
    }

    public void InitCreate()
    {
        _isEdit = false;
        Title = Translation["WoolForm_CreateTitle"];
        Name = Brand = Material = Weight = Length = string.Empty;
        SelectedNeedleRange = NeedleRanges[0];
        SelectedColor = Colors.Gray;
        AllColors.Clear();
        ErrorMessage = null;
    }

    public void InitEdit(Wool? wool)
    {
        if (wool is null)
        {
            ErrorMessage = Translation["Stocks_Errors_NoSelectedWool"];
            return;
        }
        
        _isEdit = true;
        _editingId = wool.Id;
        Title = Translation["WoolForm_EditTitle"];
        Name = wool.Name;
        Brand = wool.Brand;
        Material = wool.Material;
        Weight = wool.Weight.ToString("G");
        Length = wool.Length.ToString("G");
        AllColors.Clear();
        foreach (var color in wool.Colors)
            AllColors.Add(color);

        var range = Wool.FindContainingNeedleRange(wool.NeedleMinSize, wool.NeedleMaxSize);
        SelectedNeedleRange = range is not null
            ? FindNeedleRangeSummary(range) ?? NeedleRanges[0]
            : NeedleRanges[0];
        ErrorMessage = null;
        
        try
        {
            SelectedColor = Colors.Gray;
        }
        catch
        {
            SelectedColor = Colors.Gray;
        }
    }

    private bool CanSave() =>
        !string.IsNullOrWhiteSpace(Name) &&
        !string.IsNullOrWhiteSpace(Brand) &&
        !string.IsNullOrWhiteSpace(Material) &&
        double.TryParse(Weight, out var w) && w > 0 &&
        double.TryParse(Length, out var l) && l > 0 &&
        SelectedNeedleRange is not null;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        ErrorMessage = null;

        if (!double.TryParse(Length, out var length) || length <= 0)
        {
            ErrorMessage = Translation["WoolForm_Errors_LengthMustBePositive"];
            return;
        }
        
        if (!double.TryParse(Weight, out var weight) || weight <= 0)
        {
            ErrorMessage = Translation["WoolForm_Errors_WeightMustBePositive"];
            return;
        }

        if (SelectedNeedleRange is not { } needleRange)
        {
            ErrorMessage = Translation["WoolForm_Errors_SelectNeedleSize"];
            return;
        }

        try
        {
            IsBusy = true;
            if (_isEdit)
            {
                var result = await woolService.UpdateAsync(new UpdateWoolRequest(
                    _editingId,
                    Name,
                    Brand,
                    Material,
                    [.. AllColors],
                    weight,
                    length,
                    needleRange.NeedleRange.Min,
                    needleRange.NeedleRange.Max));
                if (result.Failed)
                {
                    ErrorMessage = result.Error;
                    notifications.Error(result.Error ?? Translation["Stocks_Notifications_UnableToSaveWool"]);
                    return;
                }

                notifications.Success(Translation["Stocks_Notifications_WoolUpdated"]);
            }
            else
            {
                var result = await woolService.AddAsync(new CreateWoolRequest(
                    Name,
                    Brand,
                    Material,
                    [.. AllColors],
                    weight,
                    length,
                    1000,
                    needleRange.NeedleRange.Min,
                    needleRange.NeedleRange.Max));
                if (result.Failed)
                {
                    ErrorMessage = result.Error;
                    notifications.Error(result.Error ?? Translation["Stocks_Notifications_UnableToCreateWool"]);
                    return;
                }

                notifications.Success(Translation["Stocks_Notifications_WoolCreated"]);
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

    private WoolNeedleRangeSummary? FindNeedleRangeSummary(WoolNeedleRange range) =>
        NeedleRanges.FirstOrDefault(r => r.NeedleRange.Matches(range.Min, range.Max));
}
