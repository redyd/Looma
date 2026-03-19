using CommunityToolkit.Mvvm.ComponentModel;

namespace Looma.Presentation.ViewModels.Base;

public abstract partial class PageViewModelBase : ViewModelBase
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _title = string.Empty;

    /// <summary>Appelé à chaque fois que la page devient active.</summary>
    public virtual void OnNavigatedTo() { }

    /// <summary>Appelé quand on quitte la page.</summary>
    public virtual void OnNavigatedFrom() { }
}