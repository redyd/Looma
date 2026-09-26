// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

namespace Looma.Domain.Integrity;

/// <param name="Kind">What is wrong.</param>
/// <param name="Target">The affected item: a file name, or a document id.</param>
/// <param name="Detail">Optional human-readable detail (e.g. document nickname or error).</param>
public sealed record IntegrityIssue(IntegrityIssueKind Kind, string Target, string? Detail = null);
