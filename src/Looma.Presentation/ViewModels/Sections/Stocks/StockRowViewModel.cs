using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Entities;

namespace Looma.Presentation.ViewModels.Sections.Stocks;

public enum StockInputMode { Weight, Length }

public partial class StockRowViewModel : ObservableObject
{
    private readonly Func<StockRowViewModel, Task> _onSave;
    private readonly Func<StockRowViewModel, Task> _onDelete;
    private readonly double _lengthToWeightRatio;

    public int StockId { get; }
    public int WoolId { get; }
    public bool IsNew { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveRowCommand))]
    [NotifyPropertyChangedFor(nameof(ComputedWeightGrams))]
    [NotifyPropertyChangedFor(nameof(ComputedLengthMeters))]
    private string _inputText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ComputedWeightGrams))]
    [NotifyPropertyChangedFor(nameof(ComputedLengthMeters))]
    [NotifyPropertyChangedFor(nameof(IsWeightMode))]
    [NotifyPropertyChangedFor(nameof(IsLengthMode))]
    private StockInputMode _inputMode = StockInputMode.Weight;

    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private string? _errorMessage;

    public bool IsWeightMode => InputMode == StockInputMode.Weight;
    public bool IsLengthMode => InputMode == StockInputMode.Length;

    public string DisplayWeight => $"{ComputedWeightGrams:N0} g";
    public string DisplayLength => $"{ComputedLengthMeters:N0} m";

    public double ComputedWeightGrams
    {
        get
        {
            if (!double.TryParse(InputText.Replace(",", "."),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var v) || v <= 0)
                return 0;
            return InputMode == StockInputMode.Weight
                ? v
                : v / _lengthToWeightRatio * 100.0;
        }
    }

    public double ComputedLengthMeters
    {
        get
        {
            if (ComputedWeightGrams <= 0) return 0;
            return ComputedWeightGrams / 100.0 * _lengthToWeightRatio;
        }
    }

    public StockRowViewModel(
        Stock stock,
        double lengthToWeightRatio,
        Func<StockRowViewModel, Task> onSave,
        Func<StockRowViewModel, Task> onDelete,
        bool isNew = false)
    {
        StockId = stock.Id;
        WoolId = stock.WoolId;
        _lengthToWeightRatio = lengthToWeightRatio;
        InputMode = StockInputMode.Weight;
        InputText = isNew ? string.Empty : stock.WeightGrams.ToString("G");
        IsEditing = isNew;
        IsNew = isNew;
        _onSave = onSave;
        _onDelete = onDelete;
    }

    private bool CanSaveRow() => ComputedWeightGrams > 0;

    [RelayCommand(CanExecute = nameof(CanSaveRow))]
    private async Task SaveRowAsync()
    {
        ErrorMessage = null;
        try { await _onSave(this); }
        catch (Exception ex) { ErrorMessage = ex.Message; }
    }

    [RelayCommand]
    private async Task DeleteRowAsync()
    {
        try { await _onDelete(this); }
        catch (Exception ex) { ErrorMessage = ex.Message; }
    }

    [RelayCommand]
    private void EditRow() => IsEditing = true;

    [RelayCommand]
    private void SetWeightMode() => InputMode = StockInputMode.Weight;

    [RelayCommand]
    private void SetLengthMode() => InputMode = StockInputMode.Length;

    public double ParsedWeight() => ComputedWeightGrams;
}