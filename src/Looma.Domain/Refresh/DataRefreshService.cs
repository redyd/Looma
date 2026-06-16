// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Logging;

namespace Looma.Domain.Refresh;

public sealed class DataRefreshService(IDomainLogger logger) : IDataRefreshService
{
    public event EventHandler<DataRefreshRequestedEventArgs>? RefreshRequested;

    public void RequestRefresh(RefreshScope scope, string reason)
    {
        if (scope == RefreshScope.None)
            return;

        logger.Log(DomainLogLevel.Information, $"Refresh requested for {scope}: {reason}.");
        RefreshRequested?.Invoke(this, new DataRefreshRequestedEventArgs(scope, reason));
    }
}
