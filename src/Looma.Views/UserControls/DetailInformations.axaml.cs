using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Looma.Presentation.UserControls;

namespace Looma.Views.UserControls;

public partial class DetailInformations : UserControl
{
    public static readonly StyledProperty<IList<StatItem>> StatsProperty =
        AvaloniaProperty.Register<DetailInformations, IList<StatItem>>(
            nameof(Stats), defaultValue: []);

    public static readonly StyledProperty<IList<InfoItem>> InfosProperty =
        AvaloniaProperty.Register<DetailInformations, IList<InfoItem>>(
            nameof(Infos), defaultValue: []);

    public static readonly StyledProperty<string> InfoTitleProperty =
        AvaloniaProperty.Register<DetailInformations, string>(nameof(InfoTitle), defaultValue: "Informations");

    public IList<StatItem> Stats
    {
        get => GetValue(StatsProperty);
        set => SetValue(StatsProperty, value);
    }

    public IList<InfoItem> Infos
    {
        get => GetValue(InfosProperty);
        set => SetValue(InfosProperty, value);
    }

    public string InfoTitle
    {
        get => GetValue(InfoTitleProperty);
        set => SetValue(InfoTitleProperty, value);
    }

    public DetailInformations()
    {
        InitializeComponent();
    }
}