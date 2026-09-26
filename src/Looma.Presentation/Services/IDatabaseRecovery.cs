// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;

namespace Looma.Presentation.Services;

public interface IDatabaseRecovery
{
    /// <summary>Moves the unreadable database aside (never deletes it) so the next start begins with an empty one.</summary>
    Result SetAsideDatabase();
}
