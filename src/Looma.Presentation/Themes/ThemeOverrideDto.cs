namespace Looma.Presentation.Themes;

public class ThemeOverrideDto
{
    public string? Name { get; set; }
    public AccentColors? Accent { get; set; }
    public PrimaryColors? Primary { get; set; }
    public TextColors? Text { get; set; }
    public BackgroundColors? Background { get; set; }
    public StateColors? State { get; set; }
    public BorderColors? Borders { get; set; }
    public ButtonColors? Buttons { get; set; }
    public FormColors? Forms { get; set; }
    public SurfaceColors? Surfaces { get; set; }
    public NavigationColors? Navigation { get; set; }
    public RibbonColors? Ribbons { get; set; }
    public ElementColors? Elements { get; set; }
}
