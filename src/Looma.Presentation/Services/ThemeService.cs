using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Looma.Presentation.Themes;

namespace Looma.Presentation.Services;

public class ThemeService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public void ApplyOverride(string jsonPath)
    {
        var json = File.ReadAllText(jsonPath);
        var dto = JsonSerializer.Deserialize<ThemeOverrideDto>(json, JsonOptions);
        if (dto is null) return;

        var resources = Application.Current!.Resources;

        ApplyGroup(resources, dto.Accent);
        ApplyGroup(resources, dto.Primary);
        ApplyGroup(resources, dto.Text);
        ApplyGroup(resources, dto.Background);
        ApplyGroup(resources, dto.State);
        ApplyGroup(resources, dto.Borders);
        ApplyGroup(resources, dto.Buttons);
        ApplyGroup(resources, dto.Forms);
        ApplyGroup(resources, dto.Surfaces);
        ApplyGroup(resources, dto.Navigation);
        ApplyGroup(resources, dto.EntityBadges);
        ApplyGroup(resources, dto.Details);
    }

    private static void ApplyGroup(IResourceDictionary resources, object? group)
    {
        if (group is null) return;

        foreach (var prop in group.GetType().GetProperties())
        {
            var value = prop.GetValue(group) as string;
            if (value is null) continue;

            if (!Color.TryParse(value, out var color))
            {
                continue;
            }

            if (prop.Name.StartsWith("SystemAccentColor"))
            {
                resources[prop.Name] = color;
            }
            else
            {
                resources[prop.Name] = new SolidColorBrush(color);
            }
        }
    }
}