// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;

namespace Looma.Domain.Entities;

public sealed record TrackedWoolMovement(
    string Id,
    DateTime Date,
    double Quantity,
    int WoolId,
    string WoolName,
    string WoolBrand,
    int? ProjectId,
    string? ProjectName,
    Status? ProjectStatus,
    PatternType? PatternType);
