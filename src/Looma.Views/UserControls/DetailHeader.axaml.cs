using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Looma.Views.UserControls;

public partial class DetailHeader : UserControl
{
    public static readonly StyledProperty<ICommand?> GoBackCommandProperty =
        AvaloniaProperty.Register<DetailHeader, ICommand?>(nameof(GoBackCommand));

    public static readonly StyledProperty<ICommand?> EditCommandProperty =
        AvaloniaProperty.Register<DetailHeader, ICommand?>(nameof(EditCommand));

    public static readonly StyledProperty<ICommand?> DeleteCommandProperty =
        AvaloniaProperty.Register<DetailHeader, ICommand?>(nameof(DeleteCommand));

    public static readonly StyledProperty<object?> TitleContentProperty =
        AvaloniaProperty.Register<DetailHeader, object?>(nameof(TitleContent));

    public ICommand? GoBackCommand
    {
        get => GetValue(GoBackCommandProperty);
        set => SetValue(GoBackCommandProperty, value);
    }

    public ICommand? EditCommand
    {
        get => GetValue(EditCommandProperty);
        set => SetValue(EditCommandProperty, value);
    }

    public ICommand? DeleteCommand
    {
        get => GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }

    public object? TitleContent
    {
        get => GetValue(TitleContentProperty);
        set => SetValue(TitleContentProperty, value);
    }

    public DetailHeader()
    {
        InitializeComponent();
    }
}