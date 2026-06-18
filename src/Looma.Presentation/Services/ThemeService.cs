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
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    private readonly Dictionary<string, object?> _defaultResources = [];

    public ThemeService()
    {
        CaptureDefaultResources();
    }

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

    public void ResetToDefault()
    {
        if (_defaultResources.Count == 0)
        {
            CaptureDefaultResources();
        }

        var resources = Application.Current!.Resources;

        foreach (var (key, value) in _defaultResources)
        {
            resources[key] = value;
        }
    }

    public void ExportCurrentOverride(string destinationPath)
    {
        var directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var dto = BuildCurrentOverride();
        var json = JsonSerializer.Serialize(dto, JsonOptions);
        File.WriteAllText(destinationPath, json);
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

    private void CaptureDefaultResources()
    {
        if (Application.Current is null) return;

        var resources = Application.Current.Resources;
        foreach (var key in EnumerateThemeKeys())
        {
            if (TryGetResourceValue(key, out var value))
            {
                _defaultResources[key] = value;
            }
        }
    }

    private static ThemeOverrideDto BuildCurrentOverride()
    {
        var resources = Application.Current!.Resources;

        return new ThemeOverrideDto
        {
            Name = "Export Looma",
            Accent = BuildGroup<AccentColors>(resources),
            Primary = BuildGroup<PrimaryColors>(resources),
            Text = BuildGroup<TextColors>(resources),
            Background = BuildGroup<BackgroundColors>(resources),
            State = BuildGroup<StateColors>(resources),
            Borders = BuildGroup<BorderColors>(resources),
            Buttons = BuildGroup<ButtonColors>(resources),
            Forms = BuildGroup<FormColors>(resources),
            Surfaces = BuildGroup<SurfaceColors>(resources),
            Navigation = BuildGroup<NavigationColors>(resources),
            EntityBadges = BuildGroup<EntityBadgeColors>(resources),
            Details = BuildGroup<DetailColors>(resources)
        };
    }

    private static T BuildGroup<T>(IResourceDictionary resources)
        where T : new()
    {
        var group = new T();

        foreach (var prop in typeof(T).GetProperties())
        {
            var key = prop.Name;
            var value = TryGetResourceValue(key, out var resourceValue)
                ? resourceValue
                : null;
            var color = value switch
            {
                Color c => c,
                ISolidColorBrush brush => brush.Color,
                _ => (Color?)null
            };

            if (color.HasValue)
            {
                prop.SetValue(group, color.Value.ToString());
            }
        }

        return group;
    }

    private static bool TryGetResourceValue(string key, out object? value)
    {
        value = null;

        if (Application.Current is null)
            return false;

        return Application.Current.TryGetResource(key, null, out value);
    }

    private static IEnumerable<string> EnumerateThemeKeys()
    {
        var groupTypes = new[]
        {
            typeof(AccentColors),
            typeof(PrimaryColors),
            typeof(TextColors),
            typeof(BackgroundColors),
            typeof(StateColors),
            typeof(BorderColors),
            typeof(ButtonColors),
            typeof(FormColors),
            typeof(SurfaceColors),
            typeof(NavigationColors),
            typeof(EntityBadgeColors),
            typeof(DetailColors)
        };

        return groupTypes.SelectMany(type => type.GetProperties().Select(prop => prop.Name));
    }
}
