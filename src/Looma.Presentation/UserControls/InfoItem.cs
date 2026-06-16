// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

namespace Looma.Presentation.UserControls;

public class InfoItem
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public List<string>? Colors { get; set; }
    public bool IsLink { get; set; }
    public bool IsSimple => Colors is null && !IsLink;
}
