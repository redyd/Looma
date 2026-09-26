// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

namespace Looma.Domain.Integrity;

public sealed record IntegrityReport(IReadOnlyList<IntegrityIssue> Issues)
{
    public static IntegrityReport Healthy { get; } = new([]);

    public bool IsHealthy => Issues.Count == 0;

    public IReadOnlyList<IntegrityIssue> Of(IntegrityIssueKind kind) =>
        Issues.Where(issue => issue.Kind == kind).ToList();
}
