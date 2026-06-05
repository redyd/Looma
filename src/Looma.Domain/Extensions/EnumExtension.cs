using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Looma.Domain.Extensions;

public static class EnumExtension
{
    public static string GetDisplayName(this Enum value) =>
        value.GetType()
            .GetField(value.ToString())!
            .GetCustomAttribute<DisplayAttribute>()?.Name ?? value.ToString();
}