// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;

namespace Looma.Domain.Request;

public record CreateProjectRequest(
    string Name,
    Status Status,
    string? Note,
    DateOnly? BeginDate,
    DateOnly? EndDate,
    int? PatternId,
    IReadOnlyList<int> WoolIds);
