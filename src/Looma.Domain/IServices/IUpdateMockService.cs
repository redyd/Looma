// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.

namespace Looma.Domain.IServices;

public interface IUpdateMockService
{
    bool IsUpdateMockEnabled { get; }
    string MockCurrentVersion { get; set; }
    string MockUpdateVersion { get; set; }
    string MockReleaseNotes { get; set; }
    bool CanSimulateRestart { get; }

    Task PublishMockUpdateAsync();
    Task SimulateRestartAsync();
}
