// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

namespace Looma.Domain.Refresh;

public sealed class DataRefreshRequestedEventArgs(RefreshScope scope, string reason)
    : EventArgs
{
    public RefreshScope Scope { get; } = scope;
    public string Reason { get; } = reason;
}
