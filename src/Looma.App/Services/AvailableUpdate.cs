// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.

namespace Looma.App.Services;

public sealed class AvailableUpdate(
    object nativeUpdate,
    string version,
    string releaseNotes)
{
    public object NativeUpdate { get; } = nativeUpdate;
    public string Version { get; } = version;
    public string ReleaseNotes { get; } = releaseNotes;
}
