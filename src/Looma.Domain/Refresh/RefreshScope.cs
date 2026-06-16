// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

namespace Looma.Domain.Refresh;

[Flags]
public enum RefreshScope
{
    None = 0,
    Wools = 1,
    Projects = 2,
    Patterns = 4,
    Documents = 8,
    All = Wools | Projects | Patterns | Documents
}
