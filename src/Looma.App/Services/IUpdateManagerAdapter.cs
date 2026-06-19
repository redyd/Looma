// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.

using System;
using System.Threading.Tasks;

namespace Looma.App.Services;

public interface IUpdateManagerAdapter
{
    bool IsInstalled { get; }

    Task<AvailableUpdate?> CheckForUpdatesAsync();
    Task DownloadUpdatesAsync(AvailableUpdate update, Action<int> progress);
    void ApplyUpdatesAndRestart(AvailableUpdate update);
}
