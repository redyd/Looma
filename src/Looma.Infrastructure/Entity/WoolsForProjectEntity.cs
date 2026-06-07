// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

namespace Looma.Infrastructure.Entity;

public class WoolsForProjectEntity
{
    public int WoolId { get; set; }
    public WoolEntity WoolEntity { get; set; } = null!;

    public int ProjectId { get; set; }
    public ProjectEntity ProjectEntity { get; set; } = null!;
}