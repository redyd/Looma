// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.

namespace Looma.Domain.Core;

public enum UpdateStatus
{
    Idle,
    Checking,
    Available,
    Downloading,
    Installing,
    Error
}
