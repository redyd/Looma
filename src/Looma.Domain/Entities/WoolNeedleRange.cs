// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;

namespace Looma.Domain.Entities;

public sealed record WoolNeedleRange(WoolType Type, double Min, double Max)
{

    public bool Contains(double needleMinSize, double needleMaxSize) =>
        needleMinSize <= Max && needleMaxSize >= Min;

    public bool Matches(double needleMinSize, double needleMaxSize) =>
        Min.Equals(needleMinSize) && Max.Equals(needleMaxSize);
}
