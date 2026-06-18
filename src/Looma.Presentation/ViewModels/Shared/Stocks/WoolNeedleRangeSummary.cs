using Looma.Domain.Entities;

namespace Looma.Presentation.ViewModels.Sections.Stocks;

public class WoolNeedleRangeSummary(WoolNeedleRange NeedleRange)
{
    public WoolNeedleRange NeedleRange { get; } = NeedleRange;
    public string Label => NeedleRange.Max == double.MaxValue
        ? $"{NeedleRange.Type} - {NeedleRange.Min:G}+ mm"
        : $"{NeedleRange.Type} - {NeedleRange.Min:G} à {NeedleRange.Max:G} mm";
}