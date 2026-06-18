using Looma.Domain.Entities;
using Looma.Domain.Extensions;

namespace Looma.Presentation.ViewModels.Sections.Stocks;

public class WoolNeedleRangeSummary(WoolNeedleRange NeedleRange)
{
    public WoolNeedleRange NeedleRange { get; } = NeedleRange;
    public string Label => NeedleRange.Max == double.MaxValue
        ? $"{NeedleRange.Type.GetDisplayName()} - {NeedleRange.Min:G}+ mm"
        : $"{NeedleRange.Type.GetDisplayName()} - {NeedleRange.Min:G} à {NeedleRange.Max:G} mm";
}