// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;

namespace Looma.Domain.Request;

public sealed record CreatePatternRequest(
    string Name,
    string? Url,
    string? Note,
    PatternType Type,
    bool IsPersonal,
    DateOnly? BeginDate = null,
    DateOnly? EndDate = null);
