// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Entities;
using Looma.Domain.Extensions;
using Looma.Presentation.Services;

namespace Looma.Presentation.ViewModels.Sections.Stocks;

public class WoolNeedleRangeSummary(WoolNeedleRange NeedleRange)
{
    public WoolNeedleRange NeedleRange { get; } = NeedleRange;
    public string Label => NeedleRange.Max == double.MaxValue
        ? $"{NeedleRange.Type.GetDisplayName()} - {NeedleRange.Min:G}+ mm"
        : TranslationService.Current.Format(
            "WoolForm_NeedleRangeLabel",
            NeedleRange.Type.GetDisplayName(),
            NeedleRange.Min,
            NeedleRange.Max);
}
